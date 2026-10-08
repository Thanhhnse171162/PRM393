using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CourtGo.Application.Bookings;
using CourtGo.Application.Operations;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using CourtGo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CourtGo.IntegrationTests;

public class StaffOperationsApiTests : IDisposable
{
    private readonly BookingHoldApiTests.Factory factory = new();
    private readonly HttpClient client;
    public StaffOperationsApiTests() { client = factory.CreateClient(); }
    public void Dispose() { client.Dispose(); factory.Dispose(); }

    private async Task<(Guid Own, Guid Other)> SeedAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        db.StaffAssignments.Add(new StaffAssignment { StaffUserId = factory.StaffUser.Id, SportCenterId = factory.CenterId });
        var court = await db.Courts.SingleAsync(c => c.Id == factory.ActiveCourtId);
        var other = new SportCenter { Name = "Other", AddressLine = "Other", City = "Other" };
        var otherCourt = new Court { SportCenter = other, SportId = court.SportId, Code = "Other", Name = "Other" };
        db.Courts.Add(otherCourt);
        var now = DateTimeOffset.UtcNow;
        Booking Make(Guid courtId, BookingStatus status, DateTimeOffset start) => new()
        {
            BookingCode = Guid.NewGuid().ToString("N")[..20], CourtId = courtId, CustomerUserId = factory.ActiveCustomer.Id,
            CustomerNameSnapshot = "Search Person", CustomerPhoneSnapshot = "0912345678",
            CourtNameSnapshot = "Snapshot Court", CenterNameSnapshot = "Snapshot Center", SportNameSnapshot = "Badminton",
            StartAt = start, EndAt = start.AddHours(1), DurationMinutes = 60, TotalAmount = 100000,
            DepositAmount = 30000, BookingStatus = status, PaymentStatus = BookingPaymentStatus.DepositPaid
        };
        var own = Make(court.Id, BookingStatus.Confirmed, now.AddMinutes(-10));
        var foreign = Make(otherCourt.Id, BookingStatus.Confirmed, now.AddMinutes(-10));
        var playing = Make(court.Id, BookingStatus.CheckedIn, now.AddMinutes(-5));
        var upcoming = Make(court.Id, BookingStatus.Confirmed, now.AddDays(1));
        db.Bookings.AddRange(own, foreign, playing, upcoming);
        db.Payments.Add(new Payment { BookingId = own.Id, Amount = 30000, PaymentKind = PaymentKind.Deposit,
            PaymentMethod = PaymentMethod.MoMo, TransactionStatus = PaymentTransactionStatus.Succeeded });
        db.BookingSlots.Add(new BookingSlot { BookingId = own.Id, CourtId = court.Id, StartAt = own.StartAt,
            EndAt = own.EndAt, UnitPrice = 100000, ReservationState = ReservationState.Reserved });
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.StaffToken);
        return (own.Id, foreign.Id);
    }

    [Fact]
    public async Task Schedule_FiltersPagesAndIsolatesCenter()
    {
        await SeedAsync();
        var all = await client.GetFromJsonAsync<PagedResult<StaffScheduleItem>>("/api/staff/bookings?pageSize=1&pageNumber=2");
        Assert.Equal(3, all!.TotalItems);
        Assert.Single(all.Items);
        var filtered = await client.GetFromJsonAsync<PagedResult<StaffScheduleItem>>("/api/staff/bookings?status=Confirmed&search=091234");
        Assert.Equal(2, filtered!.TotalItems);
        var empty = await client.GetFromJsonAsync<PagedResult<StaffScheduleItem>>("/api/staff/bookings?sportId=" + Guid.NewGuid());
        Assert.Empty(empty!.Items);
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow.AddDays(1), "SE Asia Standard Time").DateTime);
        var tomorrow = await client.GetFromJsonAsync<PagedResult<StaffScheduleItem>>("/api/staff/bookings?date=" + date.ToString("yyyy-MM-dd"));
        Assert.Single(tomorrow!.Items);
    }

    [Fact]
    public async Task Detail_ReturnsSnapshotsActualPaymentsAndRejectsOtherCenter()
    {
        var (own, other) = await SeedAsync();
        var detail = await client.GetFromJsonAsync<StaffBookingDetail>("/api/staff/bookings/" + own);
        Assert.Equal("Snapshot Court", detail!.Booking.CourtName);
        Assert.Equal(30000, detail.Payment.PaidAmount);
        Assert.Equal(70000, detail.Payment.RemainingAmount);
        Assert.Single(detail.Slots);
        Assert.Single(detail.Payments);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/staff/bookings/" + other)).StatusCode);
    }

    [Fact]
    public async Task Dashboard_CountsOnlyAssignedCenterAndBoundsLists()
    {
        await SeedAsync();
        var dashboard = await client.GetFromJsonAsync<StaffDashboardDto>("/api/staff/dashboard");
        Assert.Equal(factory.CenterId, dashboard!.CenterId);
        Assert.Equal(1, dashboard.WaitingCheckIn);
        Assert.Equal(1, dashboard.Playing);
        Assert.Equal(1, dashboard.Upcoming);
        Assert.Equal(2, dashboard.NeedsAttention);
        Assert.Contains(dashboard.CourtStatuses, c => c.CourtId == factory.ActiveCourtId && c.OperationalState == "Playing");
        Assert.True(dashboard.NextBookings.Count <= 10);
    }

    [Theory]
    [InlineData("pageNumber=0")]
    [InlineData("pageSize=101")]
    [InlineData("status=999")]
    public async Task Schedule_RejectsInvalidFilters(string query)
    {
        await SeedAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/staff/bookings?" + query)).StatusCode);
    }

    [Fact]
    public async Task UnassignedStaffAndOtherRolesCannotRead()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.StaffToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/staff/dashboard")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.ActiveCustomerToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/staff/bookings")).StatusCode);
    }

    [Fact]
    public async Task Block_PreventsHoldAndCanBeRemovedWithoutChangingCourtStatus()
    {
        await SeedAsync();
        var start = new DateTimeOffset(DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(2).AddHours(10), DateTimeKind.Unspecified), TimeSpan.FromHours(7));
        var url = $"/api/staff/courts/{factory.ActiveCourtId}/blocks";
        var created = await client.PostAsJsonAsync(url, new CreateCourtBlockRequest(start, start.AddHours(1), "Maintenance", "Floor repair"));
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var block = await created.Content.ReadFromJsonAsync<CourtBlockDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.ActiveCustomerToken);
        var hold = await client.PostAsJsonAsync("/api/bookings/hold", new { courtId = factory.ActiveCourtId, slotStartAts = new[] { start } });
        Assert.Equal(HttpStatusCode.Conflict, hold.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.StaffToken);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(url + "/" + block!.Id)).StatusCode);
        var court = await client.GetFromJsonAsync<CourtStateDto>($"/api/staff/courts/{factory.ActiveCourtId}");
        Assert.Equal("Active", court!.Status);
    }

    [Fact]
    public async Task WalkIn_IsFullyPaidWithoutCustomerAccount_AndDuplicateConflicts()
    {
        await SeedAsync();
        var start = new DateTimeOffset(DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(2).AddHours(10), DateTimeKind.Unspecified), TimeSpan.FromHours(7));
        var request = new WalkInBookingRequest("Walk in guest", "0905555555", factory.ActiveCourtId, new() { start });
        var response = await client.PostAsJsonAsync("/api/staff/bookings/walk-in", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<StaffBookingDetail>();
        Assert.Equal("Confirmed", detail!.Booking.BookingStatus);
        Assert.Equal("FullyPaid", detail.Booking.PaymentStatus);
        Assert.Equal(100000, detail.Payment.PaidAmount);
        Assert.Equal(0, detail.Payment.RemainingAmount);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == detail.Booking.BookingId);
        Assert.Null(booking.CustomerUserId);
        Assert.Equal(BookingSource.WalkIn, booking.Source);
        Assert.Equal(factory.StaffUser.Id, booking.CreatedByUserId);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/staff/bookings/walk-in", request)).StatusCode);
        var verify = await client.PostAsJsonAsync("/api/staff/check-ins/verify", new { qrToken = booking.QrToken });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
    }

    private sealed class LifecycleClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task Cancellation_UsesBookedPolicyActualCharges_PreservesSlotsAndOwnership()
    {
        var (own, _) = await SeedAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var policy = new CancellationPolicy { Name = "Booked version", Version = 1, EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-1), IsActive = false };
        policy.Rules.Add(new CancellationPolicyRule { MinHoursBeforeStart = 12, RefundPercent = 90 });
        db.CancellationPolicies.Add(policy);
        var booking = await db.Bookings.SingleAsync(b => b.Id == own);
        booking.CancellationPolicyId = policy.Id;
        booking.StartAt = DateTimeOffset.UtcNow.AddDays(2);
        booking.EndAt = booking.StartAt.AddHours(1);
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.ActiveCustomerToken);
        var url = $"/api/bookings/{own}/cancellation-requests";
        var response = await client.PostAsJsonAsync(url, new CreateCancellationRequest("Plans changed"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cancellation = await response.Content.ReadFromJsonAsync<CancellationRequestDto>();
        Assert.Equal(27000, cancellation!.CalculatedRefundAmount);
        Assert.Equal("Pending", cancellation.Status);
        var duplicate = await (await client.PostAsJsonAsync(url, new CreateCancellationRequest("Plans changed"))).Content.ReadFromJsonAsync<CancellationRequestDto>();
        Assert.Equal(cancellation.Id, duplicate!.Id);
        Assert.True(await db.BookingSlots.AnyAsync(s => s.BookingId == own && s.IsOccupying));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.SecondCustomerToken);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(url, new CreateCancellationRequest("Other"))).StatusCode);
    }

    [Fact]
    public async Task Lifecycle_RespectsTimeBoundaries_IsIdempotent_AndKeepsSlots()
    {
        var (own, _) = await SeedAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == own);
        booking.BookingStatus = BookingStatus.CheckedIn;
        await db.SaveChangesAsync();
        var clock = new LifecycleClock(booking.StartAt.AddTicks(-1));
        var service = new BookingLifecycleService(db, new BookingCommandExecutor(db), clock);
        await service.AdvanceAsync();
        Assert.Equal(BookingStatus.CheckedIn, booking.BookingStatus);
        clock.Now = booking.StartAt;
        await service.AdvanceAsync();
        Assert.Equal(BookingStatus.InProgress, booking.BookingStatus);
        clock.Now = booking.EndAt;
        await service.AdvanceAsync();
        Assert.Equal(BookingStatus.Completed, booking.BookingStatus);
        await service.AdvanceAsync();
        Assert.Equal(2, await db.BookingStatusHistories.CountAsync(h => h.BookingId == own));
        Assert.True(await db.BookingSlots.AnyAsync(s => s.BookingId == own));
    }

    [Fact]
    public async Task NoShow_OnlyAllowsConfirmedAfterEnd()
    {
        var (own, _) = await SeedAsync();
        var url = $"/api/staff/bookings/{own}/no-show";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(url, null)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var booking = await db.Bookings.SingleAsync(b => b.Id == own);
        booking.StartAt = DateTimeOffset.UtcNow.AddHours(-2);
        booking.EndAt = DateTimeOffset.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(url, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(url, null)).StatusCode);
        Assert.Equal(1, await db.BookingStatusHistories.CountAsync(h => h.BookingId == own && h.ToStatus == BookingStatus.NoShow));
    }

    [Fact]
    public async Task Block_RejectsOccupiedSlotsInvalidTypesHistoryAndOtherCenters()
    {
        var (own, _) = await SeedAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var start = DateTimeOffset.UtcNow.AddDays(3);
        db.BookingSlots.Add(new BookingSlot { BookingId = own, CourtId = factory.ActiveCourtId,
            StartAt = start, EndAt = start.AddHours(1), ReservationState = ReservationState.Reserved, IsOccupying = true });
        var historical = new CourtBlock { CourtId = factory.ActiveCourtId, StartAt = start.AddDays(-5),
            EndAt = start.AddDays(-4), Reason = "History", CreatedByUserId = factory.StaffUser.Id };
        db.CourtBlocks.Add(historical);
        await db.SaveChangesAsync();
        var url = $"/api/staff/courts/{factory.ActiveCourtId}/blocks";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(url, new CreateCourtBlockRequest(start, start.AddHours(1), "Maintenance", "Repair"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url, new CreateCourtBlockRequest(start.AddDays(1), start.AddDays(1).AddHours(1), "999", "Repair"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync(url + "/" + historical.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/staff/courts/{Guid.NewGuid()}")).StatusCode);
    }
}
