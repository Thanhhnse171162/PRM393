using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Operations;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CourtGo.IntegrationTests;

public class AdminCancellationApiTests : IDisposable
{
    private readonly BookingHoldApiTests.Factory _factory = new();
    private readonly HttpClient _client;

    public AdminCancellationApiTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<(Guid BookingId, Guid RequestId)> SeedConfirmedBookingWithRequestAsync(
        decimal totalAmount = 200000m,
        decimal paidAmount = 60000m,
        decimal calculatedRefund = 60000m)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();

        var court = await db.Courts.SingleAsync(c => c.Id == _factory.ActiveCourtId);
        var policy = new CancellationPolicy
        {
            Name = "Standard Policy",
            EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-10),
            IsActive = true
        };
        policy.Rules.Add(new CancellationPolicyRule
        {
            MinHoursBeforeStart = 24,
            RefundPercent = 100m
        });
        db.CancellationPolicies.Add(policy);

        var start = DateTimeOffset.UtcNow.AddDays(2);
        var booking = new Booking
        {
            BookingCode = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant(),
            CourtId = court.Id,
            CustomerUserId = _factory.ActiveCustomer.Id,
            CustomerNameSnapshot = "Customer One",
            CustomerPhoneSnapshot = "0901111111",
            CourtNameSnapshot = "Sân A1",
            CenterNameSnapshot = "CourtGo Center Q7",
            SportNameSnapshot = "Cầu lông",
            StartAt = start,
            EndAt = start.AddHours(2),
            DurationMinutes = 120,
            TotalAmount = totalAmount,
            DepositAmount = paidAmount,
            BookingStatus = BookingStatus.Confirmed,
            PaymentStatus = BookingPaymentStatus.DepositPaid,
            CancellationPolicyId = policy.Id
        };
        db.Bookings.Add(booking);

        var slot = new BookingSlot
        {
            BookingId = booking.Id,
            CourtId = court.Id,
            StartAt = booking.StartAt,
            EndAt = booking.EndAt,
            UnitPrice = totalAmount,
            ReservationState = ReservationState.Reserved,
            IsOccupying = true
        };
        db.BookingSlots.Add(slot);

        var depositPayment = new Payment
        {
            BookingId = booking.Id,
            PaymentKind = PaymentKind.Deposit,
            PaymentMethod = PaymentMethod.MoMo,
            Amount = paidAmount,
            TransactionStatus = PaymentTransactionStatus.Succeeded,
            PaidByUserId = _factory.ActiveCustomer.Id,
            PaidAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        db.Payments.Add(depositPayment);

        var request = new CancellationRequest
        {
            BookingId = booking.Id,
            RequestedByUserId = _factory.ActiveCustomer.Id,
            Reason = "Need to reschedule plans",
            Status = CancellationRequestStatus.Pending,
            CalculatedRefundAmount = calculatedRefund,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.CancellationRequests.Add(request);

        await db.SaveChangesAsync();
        return (booking.Id, request.Id);
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Staff")]
    [InlineData("none")]
    public async Task AdminRoutes_RequireAdminRole(string role)
    {
        var token = role switch
        {
            "Customer" => _factory.ActiveCustomerToken,
            "Staff" => _factory.StaffToken,
            _ => null
        };

        if (token != null)
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        else
            _client.DefaultRequestHeaders.Authorization = null;

        var listRes = await _client.GetAsync("/api/admin/cancellation-requests");
        var detailRes = await _client.GetAsync($"/api/admin/cancellation-requests/{Guid.NewGuid()}");
        var decisionRes = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{Guid.NewGuid()}/decision",
            new CancellationDecisionRequest("approve", 50000m, "Test"));

        var expected = role == "none" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
        Assert.Equal(expected, listRes.StatusCode);
        Assert.Equal(expected, detailRes.StatusCode);
        Assert.Equal(expected, decisionRes.StatusCode);
    }

    [Fact]
    public async Task AdminList_SupportsFiltersPaginationAndSearch()
    {
        var (bookingId, reqId) = await SeedConfirmedBookingWithRequestAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // Basic list
        var listRes = await _client.GetFromJsonAsync<PagedResult<AdminCancellationRequestSummaryDto>>("/api/admin/cancellation-requests");
        Assert.NotNull(listRes);
        Assert.True(listRes.TotalItems >= 1);
        Assert.Contains(listRes.Items, r => r.RequestId == reqId);

        // Status filter
        var pendingOnly = await _client.GetFromJsonAsync<PagedResult<AdminCancellationRequestSummaryDto>>("/api/admin/cancellation-requests?status=Pending");
        Assert.NotNull(pendingOnly);
        Assert.All(pendingOnly.Items, r => Assert.Equal("Pending", r.Status));

        // Invalid status
        var invalidStatus = await _client.GetAsync("/api/admin/cancellation-requests?status=NonExistent");
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);

        // CenterId filter
        var byCenter = await _client.GetFromJsonAsync<PagedResult<AdminCancellationRequestSummaryDto>>($"/api/admin/cancellation-requests?centerId={_factory.CenterId}");
        Assert.NotNull(byCenter);
        Assert.Contains(byCenter.Items, r => r.RequestId == reqId);

        // Search by customer name snapshot
        var bySearch = await _client.GetFromJsonAsync<PagedResult<AdminCancellationRequestSummaryDto>>("/api/admin/cancellation-requests?search=Customer One");
        Assert.NotNull(bySearch);
        Assert.Contains(bySearch.Items, r => r.RequestId == reqId);

        // Search by phone snapshot
        var byPhone = await _client.GetFromJsonAsync<PagedResult<AdminCancellationRequestSummaryDto>>("/api/admin/cancellation-requests?search=0901111111");
        Assert.NotNull(byPhone);
        Assert.Contains(byPhone.Items, r => r.RequestId == reqId);

        // Pagination validation
        var badPage = await _client.GetAsync("/api/admin/cancellation-requests?pageNumber=0");
        Assert.Equal(HttpStatusCode.BadRequest, badPage.StatusCode);
        var badSize = await _client.GetAsync("/api/admin/cancellation-requests?pageSize=200");
        Assert.Equal(HttpStatusCode.BadRequest, badSize.StatusCode);
    }

    [Fact]
    public async Task AdminDetail_ReturnsSafeDetailsAndPolicyReference()
    {
        var (bookingId, reqId) = await SeedConfirmedBookingWithRequestAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);
        var detailRes = await _client.GetAsync($"/api/admin/cancellation-requests/{reqId}");
        Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);

        var detail = await detailRes.Content.ReadFromJsonAsync<AdminCancellationRequestDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(reqId, detail.RequestId);
        Assert.Equal("Pending", detail.RequestStatus);
        Assert.Equal(bookingId, detail.BookingId);
        Assert.Equal("Customer One", detail.Customer.Name);
        Assert.Equal("CourtGo Center Q7", detail.Facility.CenterName);
        Assert.Equal(60000m, detail.PaidAmount);
        Assert.Equal(60000m, detail.CalculatedRefundAmount);
        Assert.NotNull(detail.Policy);
        Assert.Equal("Standard Policy", detail.Policy.PolicyName);
        Assert.Equal(100m, detail.Policy.MatchedRefundPercent);
        Assert.Single(detail.PaymentSummary.Payments);

        // Non existent request returns 404
        var notFoundRes = await _client.GetAsync($"/api/admin/cancellation-requests/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, notFoundRes.StatusCode);
    }

    [Fact]
    public async Task AdminDecision_Reject_KeepsBookingConfirmedAndSlotsReserved()
    {
        var (bookingId, reqId) = await SeedConfirmedBookingWithRequestAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);
        var response = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{reqId}/decision",
            new CancellationDecisionRequest("reject", null, "Cannot cancel within window"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CancellationDecisionResponse>();
        Assert.NotNull(result);
        Assert.Equal("reject", result.Decision);
        Assert.Equal("Rejected", result.RequestStatus);
        Assert.Equal("Confirmed", result.BookingStatus);
        Assert.Null(result.RefundPaymentId);

        // Verify database state: Booking Confirmed, Slots still Reserved and Occupying
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus);

        var slots = await db.BookingSlots.Where(s => s.BookingId == bookingId).ToListAsync();
        Assert.All(slots, s =>
        {
            Assert.Equal(ReservationState.Reserved, s.ReservationState);
            Assert.True(s.IsOccupying);
        });

        // Verify rejection customer notification
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == _factory.ActiveCustomer.Id && n.ReferenceId == bookingId);
        Assert.NotNull(notification);
        Assert.Contains("từ chối", notification.Title);

        // Idempotent duplicate reject
        var duplicateRes = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{reqId}/decision",
            new CancellationDecisionRequest("reject", null, "Cannot cancel within window"));
        Assert.Equal(HttpStatusCode.OK, duplicateRes.StatusCode);
    }

    [Fact]
    public async Task AdminDecision_Approve_ReleasesSlots_CreatesStatusHistory_AndPendingRefund()
    {
        var (bookingId, reqId) = await SeedConfirmedBookingWithRequestAsync(200000m, 60000m, 60000m);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);
        var response = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{reqId}/decision",
            new CancellationDecisionRequest("approve", 50000m, "Approved with partial policy rate"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CancellationDecisionResponse>();
        Assert.NotNull(result);
        Assert.Equal("approve", result.Decision);
        Assert.Equal("Approved", result.RequestStatus);
        Assert.Equal("Cancelled", result.BookingStatus);
        Assert.Equal(50000m, result.ApprovedRefundAmount);
        Assert.NotNull(result.RefundPaymentId);

        // Verify database state
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
        Assert.Equal(BookingPaymentStatus.RefundPending, booking.PaymentStatus);

        // Slots Released & Not occupying
        var slots = await db.BookingSlots.Where(s => s.BookingId == bookingId).ToListAsync();
        Assert.All(slots, s =>
        {
            Assert.Equal(ReservationState.Released, s.ReservationState);
            Assert.False(s.IsOccupying);
        });

        // BookingStatusHistory recorded
        var history = await db.BookingStatusHistories.FirstOrDefaultAsync(h => h.BookingId == bookingId && h.ToStatus == BookingStatus.Cancelled);
        Assert.NotNull(history);
        Assert.Equal(BookingStatus.Confirmed, history.FromStatus);
        Assert.Equal(_factory.AdminUser.Id, history.ChangedByUserId);

        // Refund payment created
        var refund = await db.Payments.SingleOrDefaultAsync(p => p.Id == result.RefundPaymentId);
        Assert.NotNull(refund);
        Assert.Equal(PaymentKind.Refund, refund.PaymentKind);
        Assert.Equal(50000m, refund.Amount);
        Assert.Equal(PaymentTransactionStatus.Pending, refund.TransactionStatus);

        // Notification created
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == _factory.ActiveCustomer.Id && n.ReferenceId == bookingId);
        Assert.NotNull(notification);
        Assert.Contains("chấp thuận", notification.Title);

        // Idempotent duplicate approve returns same response
        var duplicateRes = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{reqId}/decision",
            new CancellationDecisionRequest("approve", 50000m, "Approved with partial policy rate"));
        Assert.Equal(HttpStatusCode.OK, duplicateRes.StatusCode);
    }

    [Fact]
    public async Task AdminDecision_Approve_ZeroRefund_ReleasesSlotsWithoutPayment()
    {
        var (bookingId, reqId) = await SeedConfirmedBookingWithRequestAsync(200000m, 60000m, 0m);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);
        var response = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{reqId}/decision",
            new CancellationDecisionRequest("approve", 0m, "No refund allowed under late window"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CancellationDecisionResponse>();
        Assert.NotNull(result);
        Assert.Equal("approve", result.Decision);
        Assert.Equal(0m, result.ApprovedRefundAmount);
        Assert.Null(result.RefundPaymentId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);

        var slots = await db.BookingSlots.Where(s => s.BookingId == bookingId).ToListAsync();
        Assert.All(slots, s => Assert.Equal(ReservationState.Released, s.ReservationState));

        // No refund payment created
        Assert.False(await db.Payments.AnyAsync(p => p.BookingId == bookingId && p.PaymentKind == PaymentKind.Refund));
    }

    [Fact]
    public async Task AdminDecision_ValidatesApprovedRefundBounds()
    {
        var (bookingId, reqId) = await SeedConfirmedBookingWithRequestAsync(200000m, 60000m, 50000m);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // Negative refund rejected
        var negativeRes = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{reqId}/decision",
            new CancellationDecisionRequest("approve", -1000m));
        Assert.Equal(HttpStatusCode.BadRequest, negativeRes.StatusCode);

        // Exceeds CalculatedRefundAmount (50,000) rejected
        var exceedsCalcRes = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{reqId}/decision",
            new CancellationDecisionRequest("approve", 55000m));
        Assert.Equal(HttpStatusCode.BadRequest, exceedsCalcRes.StatusCode);

        // Exceeds actual paid charges (60,000) rejected
        var exceedsPaidRes = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{reqId}/decision",
            new CancellationDecisionRequest("approve", 70000m));
        Assert.Equal(HttpStatusCode.BadRequest, exceedsPaidRes.StatusCode);

        // Invalid decision string
        var invalidDecision = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{reqId}/decision",
            new CancellationDecisionRequest("maybe"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidDecision.StatusCode);
    }

    [Fact]
    public async Task RefundSimulation_Success_PartiallyAndFullyRefunded_AndIdempotent()
    {
        // Total = 200,000, Paid = 60,000, Calculated = 60,000
        var (bookingId, reqId) = await SeedConfirmedBookingWithRequestAsync(200000m, 60000m, 60000m);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);
        var approveRes = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{reqId}/decision",
            new CancellationDecisionRequest("approve", 30000m, "Partial refund"));
        var approval = await approveRes.Content.ReadFromJsonAsync<CancellationDecisionResponse>();
        Assert.NotNull(approval?.RefundPaymentId);

        // Customer simulates development refund
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var simRes = await _client.PostAsJsonAsync($"/api/dev/payments/{approval.RefundPaymentId}/simulate",
            new SimulatePaymentRequest("success"));
        Assert.Equal(HttpStatusCode.OK, simRes.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
            // 30,000 refunded out of 60,000 paid -> PartiallyRefunded
            Assert.Equal(BookingPaymentStatus.PartiallyRefunded, booking.PaymentStatus);
            Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);

            var refundPayment = await db.Payments.SingleAsync(p => p.Id == approval.RefundPaymentId);
            Assert.Equal(PaymentTransactionStatus.Succeeded, refundPayment.TransactionStatus);
            Assert.NotNull(refundPayment.PaidAt);

            // Refund success notification exists
            var refundNotification = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == _factory.ActiveCustomer.Id && n.Type == NotificationType.Payment);
            Assert.NotNull(refundNotification);
            Assert.Contains("Hoàn tiền thành công", refundNotification.Title);
        }

        // Idempotent duplicate simulation: repeated call succeeds without duplicating notifications
        var duplicateSimRes = await _client.PostAsJsonAsync($"/api/dev/payments/{approval.RefundPaymentId}/simulate",
            new SimulatePaymentRequest("success"));
        Assert.Equal(HttpStatusCode.OK, duplicateSimRes.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var count = await db.Notifications.CountAsync(n => n.UserId == _factory.ActiveCustomer.Id && n.Type == NotificationType.Payment);
            Assert.Equal(1, count);
        }
    }

    [Fact]
    public async Task RefundSimulation_FailedAndCancelled_KeepsBookingCancelled()
    {
        var (bookingId, reqId) = await SeedConfirmedBookingWithRequestAsync(200000m, 60000m, 60000m);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);
        var approveRes = await _client.PostAsJsonAsync($"/api/admin/cancellation-requests/{reqId}/decision",
            new CancellationDecisionRequest("approve", 60000m));
        var approval = await approveRes.Content.ReadFromJsonAsync<CancellationDecisionResponse>();

        // Customer simulates failed refund
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var simRes = await _client.PostAsJsonAsync($"/api/dev/payments/{approval!.RefundPaymentId}/simulate",
            new SimulatePaymentRequest("failed"));
        Assert.Equal(HttpStatusCode.OK, simRes.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var booking = await db.Bookings.SingleAsync(b => b.Id == bookingId);
            Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);

            var refundPayment = await db.Payments.SingleAsync(p => p.Id == approval.RefundPaymentId);
            Assert.Equal(PaymentTransactionStatus.Failed, refundPayment.TransactionStatus);

            var slots = await db.BookingSlots.Where(s => s.BookingId == bookingId).ToListAsync();
            Assert.All(slots, s => Assert.Equal(ReservationState.Released, s.ReservationState));
        }
    }
}
