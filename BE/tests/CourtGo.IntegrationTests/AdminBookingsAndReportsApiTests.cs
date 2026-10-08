using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CourtGo.Application.AdminBookings;
using CourtGo.Application.AdminDashboard;
using CourtGo.Application.AdminReports;
using CourtGo.Application.Bookings;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CourtGo.IntegrationTests;

public class AdminBookingsAndReportsApiTests : IDisposable
{
    private readonly BookingHoldApiTests.Factory _factory = new();
    private readonly HttpClient _client;

    public AdminBookingsAndReportsApiTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Staff")]
    [InlineData("none")]
    public async Task AdminEndpoints_RequireAdminRole(string role)
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

        var dummyGuid = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var requests = new[]
        {
            _client.GetAsync("/api/admin/bookings"),
            _client.GetAsync($"/api/admin/bookings/{dummyGuid}"),
            _client.PostAsJsonAsync($"/api/admin/bookings/{dummyGuid}/cancel", new AdminCancelBookingRequest("test")),
            _client.GetAsync("/api/admin/dashboard"),
            _client.GetAsync($"/api/admin/reports/summary?dateFrom={today}&dateTo={today}"),
            _client.GetAsync($"/api/admin/reports/revenue?dateFrom={today}&dateTo={today}"),
            _client.GetAsync($"/api/admin/reports/bookings?dateFrom={today}&dateTo={today}"),
            _client.GetAsync($"/api/admin/reports/occupancy?dateFrom={today}&dateTo={today}")
        };

        foreach (var req in requests)
        {
            var res = await req;
            var expected = role == "none" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
            Assert.Equal(expected, res.StatusCode);
        }
    }

    [Fact]
    public async Task AdminBookings_List_Filtering_And_Detail_WorkCorrectly()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // 1. Create a confirmed booking with successful payment
        var booking = await CreateTestBookingAsync(BookingStatus.Confirmed, BookingPaymentStatus.DepositPaid, 200_000m, 60_000m);

        // 2. Query booking list
        var listRes = await _client.GetAsync("/api/admin/bookings?pageSize=10");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var paged = await listRes.Content.ReadFromJsonAsync<PagedResult<AdminBookingListItemResponse>>();
        Assert.NotNull(paged);
        Assert.NotEmpty(paged.Items);

        // Filter by booking status
        var filterRes = await _client.GetAsync("/api/admin/bookings?status=Confirmed");
        Assert.Equal(HttpStatusCode.OK, filterRes.StatusCode);
        var pagedFiltered = await filterRes.Content.ReadFromJsonAsync<PagedResult<AdminBookingListItemResponse>>();
        Assert.NotNull(pagedFiltered);
        Assert.All(pagedFiltered.Items, b => Assert.Equal("Confirmed", b.BookingStatus));

        // Filter by search
        var searchRes = await _client.GetAsync($"/api/admin/bookings?search={booking.BookingCode}");
        Assert.Equal(HttpStatusCode.OK, searchRes.StatusCode);
        var pagedSearch = await searchRes.Content.ReadFromJsonAsync<PagedResult<AdminBookingListItemResponse>>();
        Assert.NotNull(pagedSearch);
        Assert.Contains(pagedSearch.Items, b => b.BookingCode == booking.BookingCode);

        // 3. Query booking detail
        var detailRes = await _client.GetAsync($"/api/admin/bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);
        var detail = await detailRes.Content.ReadFromJsonAsync<AdminBookingDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal(booking.Id, detail.BookingId);
        Assert.Equal(booking.BookingCode, detail.BookingCode);
        Assert.Equal(200_000m, detail.FinancialSummary.TotalAmount);
        Assert.Equal(60_000m, detail.FinancialSummary.PaidAmount);
        Assert.Equal(140_000m, detail.FinancialSummary.RemainingAmount);
        Assert.NotEmpty(detail.Slots);
        Assert.NotEmpty(detail.Payments);
    }

    [Fact]
    public async Task AdminBookings_DirectCancel_ReleasesSlots_And_CreatesRefund()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        var booking = await CreateTestBookingAsync(BookingStatus.Confirmed, BookingPaymentStatus.DepositPaid, 150_000m, 45_000m);

        // Cancel directly as Admin
        var cancelRequest = new AdminCancelBookingRequest("Sân ngập nước khẩn cấp", 45_000m);
        var cancelRes = await _client.PostAsJsonAsync($"/api/admin/bookings/{booking.Id}/cancel", cancelRequest);
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);

        var cancelResponse = await cancelRes.Content.ReadFromJsonAsync<AdminCancelBookingResponse>();
        Assert.NotNull(cancelResponse);
        Assert.Equal(booking.Id, cancelResponse.BookingId);
        Assert.Equal("Cancelled", cancelResponse.BookingStatus);
        Assert.Equal(45_000m, cancelResponse.RefundAmount);

        // Verify slots released in database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGo.Infrastructure.Data.CourtGoDbContext>();
        var slots = await db.BookingSlots.Where(s => s.BookingId == booking.Id).ToListAsync();
        Assert.All(slots, s => Assert.Equal(ReservationState.Released, s.ReservationState));

        // Verify status history created
        var histories = await db.BookingStatusHistories.Where(h => h.BookingId == booking.Id).ToListAsync();
        Assert.Contains(histories, h => h.ToStatus == BookingStatus.Cancelled && h.Reason == "Sân ngập nước khẩn cấp");

        // Verify cannot cancel again
        var repeatRes = await _client.PostAsJsonAsync($"/api/admin/bookings/{booking.Id}/cancel", cancelRequest);
        Assert.Equal(HttpStatusCode.Conflict, repeatRes.StatusCode);
    }

    [Fact]
    public async Task AdminDashboard_ReturnsAggregates_And_Revenues()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await CreateTestBookingAsync(BookingStatus.Confirmed, BookingPaymentStatus.DepositPaid, 300_000m, 90_000m);

        var res = await _client.GetAsync($"/api/admin/dashboard?date={today:yyyy-MM-dd}&centerId={_factory.CenterId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var dashboard = await res.Content.ReadFromJsonAsync<AdminDashboardResponse>();
        Assert.NotNull(dashboard);
        Assert.Equal(today, dashboard.Date);
        Assert.Equal(_factory.CenterId, dashboard.CenterId);
        Assert.True(dashboard.TotalBookings >= 1);
        Assert.True(dashboard.TodayRevenue >= 90_000m);
        Assert.True(dashboard.ActiveCenters >= 1);
        Assert.True(dashboard.ActiveCourts >= 1);
    }

    [Fact]
    public async Task AdminReports_Summary_And_Revenue_ExcludeUnsuccessfulPayments()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var booking = await CreateTestBookingAsync(BookingStatus.Confirmed, BookingPaymentStatus.DepositPaid, 500_000m, 150_000m);

        // Add a Failed payment and a Cancelled payment to verify they are excluded from revenue
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGo.Infrastructure.Data.CourtGoDbContext>();
            db.Payments.Add(new Payment
            {
                BookingId = booking.Id,
                PaymentKind = PaymentKind.Remaining,
                PaymentMethod = PaymentMethod.MoMo,
                Amount = 350_000m,
                TransactionStatus = PaymentTransactionStatus.Failed,
                CreatedAt = DateTimeOffset.UtcNow
            });
            db.Payments.Add(new Payment
            {
                BookingId = booking.Id,
                PaymentKind = PaymentKind.Remaining,
                PaymentMethod = PaymentMethod.VNPay,
                Amount = 350_000m,
                TransactionStatus = PaymentTransactionStatus.Cancelled,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Test Summary Report
        var summaryRes = await _client.GetAsync($"/api/admin/reports/summary?dateFrom={today:yyyy-MM-dd}&dateTo={today:yyyy-MM-dd}&centerId={_factory.CenterId}");
        Assert.Equal(HttpStatusCode.OK, summaryRes.StatusCode);
        var summary = await summaryRes.Content.ReadFromJsonAsync<AdminReportSummaryResponse>();
        Assert.NotNull(summary);
        Assert.True(summary.GrossCollected >= 150_000m);
        Assert.Equal(summary.GrossCollected - summary.RefundAmount, summary.NetRevenue);

        // Test Revenue Report
        var revRes = await _client.GetAsync($"/api/admin/reports/revenue?dateFrom={today:yyyy-MM-dd}&dateTo={today:yyyy-MM-dd}&groupBy=day");
        Assert.Equal(HttpStatusCode.OK, revRes.StatusCode);
        var revenue = await revRes.Content.ReadFromJsonAsync<AdminRevenueReportResponse>();
        Assert.NotNull(revenue);
        Assert.NotEmpty(revenue.Items);
        Assert.Equal(revenue.GrossCollected - revenue.RefundAmount, revenue.NetRevenue);

        // Test Invalid Date Range
        var tomorrow = today.AddDays(1);
        var invalidRes = await _client.GetAsync($"/api/admin/reports/summary?dateFrom={tomorrow:yyyy-MM-dd}&dateTo={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.BadRequest, invalidRes.StatusCode);
    }

    [Fact]
    public async Task AdminReports_Bookings_And_Occupancy_ReturnAccurateMetrics()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await CreateTestBookingAsync(BookingStatus.Completed, BookingPaymentStatus.FullyPaid, 200_000m, 200_000m);

        // Test Bookings Report
        var bookingsRes = await _client.GetAsync($"/api/admin/reports/bookings?dateFrom={today:yyyy-MM-dd}&dateTo={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, bookingsRes.StatusCode);
        var bookingReport = await bookingsRes.Content.ReadFromJsonAsync<AdminBookingReportResponse>();
        Assert.NotNull(bookingReport);
        Assert.True(bookingReport.TotalBookings >= 1);
        Assert.NotEmpty(bookingReport.ByStatus);
        Assert.NotEmpty(bookingReport.Trends);

        // Test Occupancy Report
        var occRes = await _client.GetAsync($"/api/admin/reports/occupancy?dateFrom={today:yyyy-MM-dd}&dateTo={today:yyyy-MM-dd}&centerId={_factory.CenterId}");
        Assert.Equal(HttpStatusCode.OK, occRes.StatusCode);
        var occReport = await occRes.Content.ReadFromJsonAsync<AdminOccupancyReportResponse>();
        Assert.NotNull(occReport);
        Assert.True(occReport.TotalBookableMinutes > 0);
        Assert.NotEmpty(occReport.ByCourt);
        Assert.NotEmpty(occReport.ByDay);
    }

    private async Task<Booking> CreateTestBookingAsync(
        BookingStatus status, BookingPaymentStatus paymentStatus, decimal totalAmount, decimal paidAmount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGo.Infrastructure.Data.CourtGoDbContext>();

        var court = await db.Courts.Include(c => c.Sport).Include(c => c.SportCenter).FirstAsync(c => c.Id == _factory.ActiveCourtId);
        var customer = await db.Users.FirstAsync(u => u.Role == UserRole.Customer);
        var now = DateTimeOffset.UtcNow;
        var startAt = now.Date.AddHours(14);
        var endAt = startAt.AddHours(2);

        var booking = new Booking
        {
            BookingCode = $"TEST-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            CustomerUserId = customer.Id,
            CustomerNameSnapshot = customer.FullName,
            CustomerPhoneSnapshot = customer.PhoneNumber,
            CustomerEmailSnapshot = customer.Email,
            CourtId = court.Id,
            CourtNameSnapshot = court.Name,
            CenterNameSnapshot = court.SportCenter!.Name,
            SportNameSnapshot = court.Sport!.Name,
            Source = BookingSource.Online,
            StartAt = startAt,
            EndAt = endAt,
            DurationMinutes = 120,
            TotalAmount = totalAmount,
            DepositPercentSnapshot = 30m,
            DepositAmount = 60_000m,
            BookingStatus = status,
            PaymentStatus = paymentStatus,
            CreatedAt = now,
            CreatedByUserId = customer.Id
        };

        db.Bookings.Add(booking);

        var slot = new BookingSlot
        {
            BookingId = booking.Id,
            CourtId = court.Id,
            StartAt = startAt,
            EndAt = endAt,
            UnitPrice = totalAmount,
            ReservationState = status == BookingStatus.Cancelled ? ReservationState.Released : ReservationState.Reserved,
            IsOccupying = status != BookingStatus.Cancelled
        };
        db.BookingSlots.Add(slot);

        if (paidAmount > 0)
        {
            var payment = new Payment
            {
                BookingId = booking.Id,
                PaymentKind = PaymentKind.Deposit,
                PaymentMethod = PaymentMethod.MoMo,
                Amount = paidAmount,
                TransactionStatus = PaymentTransactionStatus.Succeeded,
                PaidAt = now,
                CreatedAt = now,
                PaidByUserId = customer.Id
            };
            db.Payments.Add(payment);
        }

        await db.SaveChangesAsync();
        return booking;
    }
}
