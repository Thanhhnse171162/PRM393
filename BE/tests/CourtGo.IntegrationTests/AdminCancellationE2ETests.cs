using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Notifications;
using CourtGo.Application.Operations;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CourtGo.IntegrationTests;

public class AdminCancellationE2ETests : IDisposable
{
    private readonly BookingHoldApiTests.Factory _factory = new();
    private readonly HttpClient _client;

    public AdminCancellationE2ETests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task CompleteE2E_CustomerConfirmedBooking_ToCancellation_ToAdminApprove_ToRefundSimulation_ToNotification()
    {
        // 0. Seed active cancellation policy before workflow starts
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var policy = new CancellationPolicy
            {
                Name = "E2E Cancellation Policy",
                IsActive = true,
                EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-30)
            };
            policy.Rules.Add(new CancellationPolicyRule
            {
                MinHoursBeforeStart = 24,
                RefundPercent = 100m
            });
            db.CancellationPolicies.Add(policy);
            await db.SaveChangesAsync();
        }

        // 1. Customer creates a booking hold via API (5 days in the future, 14:00)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var slotTime = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(14, 0)), TimeSpan.FromHours(7));
        var holdReq = new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { slotTime });
        var holdRes = await _client.PostAsJsonAsync("/api/bookings/hold", holdReq);
        Assert.Equal(HttpStatusCode.Created, holdRes.StatusCode);
        var hold = await holdRes.Content.ReadFromJsonAsync<BookingHoldResponse>();
        Assert.NotNull(hold);
        var bookingId = hold.BookingId;

        // 2. Customer starts deposit payment via API
        var depositStartRes = await _client.PostAsJsonAsync($"/api/bookings/{bookingId}/payments/deposit",
            new StartDepositPaymentRequest("MoMo"));
        Assert.Equal(HttpStatusCode.OK, depositStartRes.StatusCode);
        var deposit = await depositStartRes.Content.ReadFromJsonAsync<DepositPaymentResponse>();
        Assert.NotNull(deposit);
        Assert.True(deposit.Amount > 0);

        // 3. Customer simulates successful deposit payment via development payment endpoint
        var depositSimRes = await _client.PostAsJsonAsync($"/api/dev/payments/{deposit.PaymentId}/simulate",
            new SimulatePaymentRequest("success"));
        Assert.Equal(HttpStatusCode.OK, depositSimRes.StatusCode);
        var depositConfirm = await depositSimRes.Content.ReadFromJsonAsync<DepositConfirmationResponse>();
        Assert.NotNull(depositConfirm);
        Assert.Equal("Confirmed", depositConfirm.BookingStatus);
        Assert.Equal("DepositPaid", depositConfirm.PaymentStatus);

        // 4. Customer submits a cancellation request via API
        var cancelReqRes = await _client.PostAsJsonAsync($"/api/bookings/{bookingId}/cancellation-requests",
            new CreateCancellationRequest("Schedule conflict with family"));
        Assert.Equal(HttpStatusCode.OK, cancelReqRes.StatusCode);
        var cancelRequest = await cancelReqRes.Content.ReadFromJsonAsync<CancellationRequestDto>();
        Assert.NotNull(cancelRequest);
        Assert.Equal("Pending", cancelRequest.Status);
        Assert.True(cancelRequest.CalculatedRefundAmount > 0);
        var requestId = cancelRequest.Id;

        // 5. Verify booking slots are NOT released yet (still occupied while pending)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var pendingSlots = await db.BookingSlots.Where(s => s.BookingId == bookingId).ToListAsync();
            Assert.NotEmpty(pendingSlots);
            Assert.All(pendingSlots, s =>
            {
                Assert.Equal(ReservationState.Reserved, s.ReservationState);
                Assert.True(s.IsOccupying);
            });
        }

        // 6. Admin logs in / sets Admin authentication header
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // 7. Admin lists cancellation requests via API
        var adminListRes = await _client.GetFromJsonAsync<PagedResult<AdminCancellationRequestSummaryDto>>("/api/admin/cancellation-requests?status=Pending");
        Assert.NotNull(adminListRes);
        Assert.Contains(adminListRes.Items, r => r.RequestId == requestId);

        // 8. Admin gets request detail via API
        var adminDetailRes = await _client.GetAsync($"/api/admin/cancellation-requests/{requestId}");
        Assert.Equal(HttpStatusCode.OK, adminDetailRes.StatusCode);
        var adminDetail = await adminDetailRes.Content.ReadFromJsonAsync<AdminCancellationRequestDetailDto>();
        Assert.NotNull(adminDetail);
        Assert.Equal(bookingId, adminDetail.BookingId);
        Assert.Equal("Pending", adminDetail.RequestStatus);
        Assert.Equal(cancelRequest.CalculatedRefundAmount, adminDetail.CalculatedRefundAmount);

        // 9. Admin approves cancellation request via API
        var decisionRes = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{requestId}/decision",
            new CancellationDecisionRequest("approve", cancelRequest.CalculatedRefundAmount, "Approved per full-refund policy"));
        Assert.Equal(HttpStatusCode.OK, decisionRes.StatusCode);
        var decision = await decisionRes.Content.ReadFromJsonAsync<CancellationDecisionResponse>();
        Assert.NotNull(decision);
        Assert.Equal("approve", decision.Decision);
        Assert.Equal("Approved", decision.RequestStatus);
        Assert.Equal("Cancelled", decision.BookingStatus);
        Assert.NotNull(decision.RefundPaymentId);
        var refundPaymentId = decision.RefundPaymentId.Value;

        // 10. Verify Booking is Cancelled and BookingSlots are Released (not occupying)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var cancelledBooking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
            Assert.Equal(BookingStatus.Cancelled, cancelledBooking.BookingStatus);
            Assert.Equal(BookingPaymentStatus.RefundPending, cancelledBooking.PaymentStatus);

            var releasedSlots = await db.BookingSlots.Where(s => s.BookingId == bookingId).ToListAsync();
            Assert.NotEmpty(releasedSlots);
            Assert.All(releasedSlots, s =>
            {
                Assert.Equal(ReservationState.Released, s.ReservationState);
                Assert.False(s.IsOccupying);
            });
        }

        // 11. Customer simulates development refund success via API
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var refundSimRes = await _client.PostAsJsonAsync($"/api/dev/payments/{refundPaymentId}/simulate",
            new SimulatePaymentRequest("success"));
        Assert.Equal(HttpStatusCode.OK, refundSimRes.StatusCode);

        // Verify booking payment status updated to Refunded
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var finalizedBooking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
            Assert.Equal(BookingStatus.Cancelled, finalizedBooking.BookingStatus);
            Assert.Equal(BookingPaymentStatus.Refunded, finalizedBooking.PaymentStatus);

            var refundPayment = await db.Payments.SingleAsync(p => p.Id == refundPaymentId);
            Assert.Equal(PaymentTransactionStatus.Succeeded, refundPayment.TransactionStatus);
        }

        // 12. Customer retrieves notifications via API
        var notificationsRes = await _client.GetFromJsonAsync<PagedResult<NotificationDto>>("/api/notifications");
        Assert.NotNull(notificationsRes);
        Assert.True(notificationsRes.TotalItems >= 2);
        Assert.Contains(notificationsRes.Items, n => n.Title.Contains("chấp thuận") || n.Title.Contains("xác nhận"));
        Assert.Contains(notificationsRes.Items, n => n.Title.Contains("Hoàn tiền thành công"));
    }
}
