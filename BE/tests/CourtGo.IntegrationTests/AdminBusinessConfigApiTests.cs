using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CourtGo.Application.Availability;
using CourtGo.Application.Bookings;
using CourtGo.Application.Cancellation;
using CourtGo.Application.Common;
using CourtGo.Application.OperatingHours;
using CourtGo.Application.PriceRules;
using CourtGo.Application.Settings;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CourtGo.IntegrationTests;

public class AdminBusinessConfigApiTests : IDisposable
{
    private readonly BookingHoldApiTests.Factory _factory = new();
    private readonly HttpClient _client;

    public AdminBusinessConfigApiTests()
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
    public async Task AdminBusinessConfigEndpoints_RequireAdminRole(string role)
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

        var routes = new[]
        {
            _client.GetAsync($"/api/admin/sport-centers/{_factory.CenterId}/operating-hours"),
            _client.PutAsJsonAsync($"/api/admin/sport-centers/{_factory.CenterId}/operating-hours",
                new UpdateOperatingHoursRequest(new[] { new UpdateOperatingHourItemRequest(CourtGoDayOfWeek.Monday, false, new TimeOnly(7, 0), new TimeOnly(22, 0)) })),
            _client.GetAsync($"/api/admin/sport-centers/{_factory.CenterId}/operating-hour-exceptions"),
            _client.PostAsJsonAsync($"/api/admin/sport-centers/{_factory.CenterId}/operating-hour-exceptions",
                new CreateOperatingHourExceptionRequest(new DateOnly(2026, 12, 25), true)),
            _client.GetAsync($"/api/admin/courts/{_factory.ActiveCourtId}/price-rules"),
            _client.PostAsJsonAsync($"/api/admin/courts/{_factory.ActiveCourtId}/price-rules",
                new CreatePriceRuleRequest(CourtGoDayOfWeek.Monday, new TimeOnly(18, 0), new TimeOnly(21, 0), 200000m)),
            _client.GetAsync("/api/admin/system-settings"),
            _client.PutAsJsonAsync("/api/admin/system-settings",
                new UpdateSystemSettingsRequest(10, 60, 30m, false)),
            _client.GetAsync("/api/admin/cancellation-policies"),
            _client.PostAsJsonAsync("/api/admin/cancellation-policies",
                new CreateCancellationPolicyRequest("Test", null, new[] { new CreateCancellationPolicyRuleRequest(0, null, 100m) })),
            _client.PostAsync($"/api/admin/cancellation-policies/{dummyGuid}/activate", null)
        };

        var responses = await Task.WhenAll(routes);
        var expected = role == "none" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;

        foreach (var res in responses)
        {
            Assert.Equal(expected, res.StatusCode);
        }
    }

    [Fact]
    public async Task OperatingHours_Get_Update_Validate_And_AvailabilityRegression()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // 1. GET operating hours returns 7 days
        var getRes = await _client.GetAsync($"/api/admin/sport-centers/{_factory.CenterId}/operating-hours");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var hoursDto = await getRes.Content.ReadFromJsonAsync<AdminCenterOperatingHoursDto>();
        Assert.NotNull(hoursDto);
        Assert.Equal(7, hoursDto.Days.Count);

        // 2. Reject invalid open/close times (open >= close)
        var invalidRes = await _client.PutAsJsonAsync($"/api/admin/sport-centers/{_factory.CenterId}/operating-hours",
            new UpdateOperatingHoursRequest(new[]
            {
                new UpdateOperatingHourItemRequest(CourtGoDayOfWeek.Monday, false, new TimeOnly(22, 0), new TimeOnly(7, 0))
            }));
        Assert.Equal(HttpStatusCode.BadRequest, invalidRes.StatusCode);

        // 3. Update hours for Monday (08:00 to 20:00)
        var updateRes = await _client.PutAsJsonAsync($"/api/admin/sport-centers/{_factory.CenterId}/operating-hours",
            new UpdateOperatingHoursRequest(new[]
            {
                new UpdateOperatingHourItemRequest(CourtGoDayOfWeek.Monday, false, new TimeOnly(8, 0), new TimeOnly(20, 0))
            }));
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

        // 4. Availability Regression: Customer availability reflects new hours
        // Find next Monday date
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        while (targetDate.DayOfWeek != DayOfWeek.Monday)
        {
            targetDate = targetDate.AddDays(1);
        }

        var availRes = await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date={targetDate:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, availRes.StatusCode);
        var avail = await availRes.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        Assert.NotNull(avail);
        Assert.False(avail.IsClosed);
        Assert.NotNull(avail.OpeningHours);
        Assert.Equal("08:00", avail.OpeningHours.OpenTime);
        Assert.Equal("20:00", avail.OpeningHours.CloseTime);
        Assert.NotEmpty(avail.Slots);
        var tz = TimeZoneHelper.ResolveTimeZone("Asia/Ho_Chi_Minh");
        Assert.Equal("08:00", TimeZoneInfo.ConvertTime(avail.Slots.First().StartAt, tz).ToString("HH:mm"));
        Assert.Equal("19:00", TimeZoneInfo.ConvertTime(avail.Slots.Last().StartAt, tz).ToString("HH:mm"));
    }

    [Fact]
    public async Task OperatingHourExceptions_Lifecycle_And_AvailabilityRegression()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);
        var holidayDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        // 1. Create Holiday Exception (Closed all day)
        var createRes = await _client.PostAsJsonAsync($"/api/admin/sport-centers/{_factory.CenterId}/operating-hour-exceptions",
            new CreateOperatingHourExceptionRequest(holidayDate, true, Reason: "National Holiday"));
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await createRes.Content.ReadFromJsonAsync<AdminOperatingHourExceptionDto>();
        Assert.NotNull(created);
        Assert.True(created.IsClosed);

        // 2. Conflict on duplicate date
        var dupRes = await _client.PostAsJsonAsync($"/api/admin/sport-centers/{_factory.CenterId}/operating-hour-exceptions",
            new CreateOperatingHourExceptionRequest(holidayDate, true));
        Assert.Equal(HttpStatusCode.Conflict, dupRes.StatusCode);

        // 3. Availability regression for holiday date: IsClosed = true, 0 slots
        var availRes = await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date={holidayDate:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, availRes.StatusCode);
        var avail = await availRes.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        Assert.NotNull(avail);
        Assert.True(avail.IsClosed);
        Assert.Empty(avail.Slots);

        // 4. Update Exception: special opening hours 10:00 to 14:00
        var updateRes = await _client.PutAsJsonAsync(
            $"/api/admin/sport-centers/{_factory.CenterId}/operating-hour-exceptions/{created.ExceptionId}",
            new UpdateOperatingHourExceptionRequest(false, new TimeOnly(10, 0), new TimeOnly(14, 0), "Half-day open"));
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

        // 5. Availability reflects special opening window 10:00 - 14:00 (4 1-hour slots)
        var specialAvailRes = await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date={holidayDate:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, specialAvailRes.StatusCode);
        var specialAvail = await specialAvailRes.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        Assert.NotNull(specialAvail);
        Assert.False(specialAvail.IsClosed);
        Assert.Equal("10:00", specialAvail.OpeningHours!.OpenTime);
        Assert.Equal("14:00", specialAvail.OpeningHours.CloseTime);
        Assert.Equal(4, specialAvail.Slots.Count);

        // 6. Delete exception
        var deleteRes = await _client.DeleteAsync(
            $"/api/admin/sport-centers/{_factory.CenterId}/operating-hour-exceptions/{created.ExceptionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // 7. Availability returns to regular weekly hours
        var restoredAvailRes = await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date={holidayDate:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, restoredAvailRes.StatusCode);
        var restoredAvail = await restoredAvailRes.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        Assert.NotNull(restoredAvail);
        Assert.False(restoredAvail.IsClosed);
    }

    [Fact]
    public async Task PriceRules_Create_OverlapRejection_And_Availability_BookingHold_Regression()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // Target Friday 2 weeks out
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
        while (targetDate.DayOfWeek != DayOfWeek.Friday)
        {
            targetDate = targetDate.AddDays(1);
        }

        // 1. Create Peak Price Rule for Friday 18:00 to 21:00 at 300,000 VND
        var createRes = await _client.PostAsJsonAsync($"/api/admin/courts/{_factory.ActiveCourtId}/price-rules",
            new CreatePriceRuleRequest(
                CourtGoDayOfWeek.Friday,
                new TimeOnly(18, 0),
                new TimeOnly(21, 0),
                300000m,
                IsActive: true));
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var createdRule = await createRes.Content.ReadFromJsonAsync<AdminPriceRuleDto>();
        Assert.NotNull(createdRule);
        Assert.Equal(300000m, createdRule.PricePerHour);

        // 2. Reject overlapping active price rule (Friday 20:00 to 22:00 overlaps 18:00-21:00)
        var overlapRes = await _client.PostAsJsonAsync($"/api/admin/courts/{_factory.ActiveCourtId}/price-rules",
            new CreatePriceRuleRequest(
                CourtGoDayOfWeek.Friday,
                new TimeOnly(20, 0),
                new TimeOnly(22, 0),
                350000m,
                IsActive: true));
        Assert.Equal(HttpStatusCode.Conflict, overlapRes.StatusCode);

        // 3. Availability regression: Friday slots between 18:00 and 21:00 have price 300,000
        var availRes = await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date={targetDate:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, availRes.StatusCode);
        var avail = await availRes.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        Assert.NotNull(avail);

        var tz = TimeZoneHelper.ResolveTimeZone("Asia/Ho_Chi_Minh");
        var peakSlot = avail.Slots.FirstOrDefault(s => TimeZoneInfo.ConvertTime(s.StartAt, tz).ToString("HH:mm") == "18:00");
        Assert.NotNull(peakSlot);
        Assert.Equal(300000m, peakSlot.Price);

        // Non-peak slot uses court base price (100,000)
        var regularSlot = avail.Slots.FirstOrDefault(s => TimeZoneInfo.ConvertTime(s.StartAt, tz).ToString("HH:mm") == "14:00");
        Assert.NotNull(regularSlot);
        Assert.Equal(100000m, regularSlot.Price);

        // 4. Booking Hold regression: Customer holds the 18:00 slot, authoritative price is 300,000
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var holdRes = await _client.PostAsJsonAsync("/api/bookings/hold",
            new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { peakSlot.StartAt }));
        Assert.Equal(HttpStatusCode.Created, holdRes.StatusCode);
        var hold = await holdRes.Content.ReadFromJsonAsync<BookingHoldResponse>();
        Assert.NotNull(hold);
        Assert.Equal(300000m, hold.TotalAmount);
    }

    [Fact]
    public async Task SystemSettings_Get_Update_Validate_And_Hold_Regression()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // 1. GET Settings
        var getRes = await _client.GetAsync("/api/admin/system-settings");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var current = await getRes.Content.ReadFromJsonAsync<SystemSettingsDto>();
        Assert.NotNull(current);

        // 2. Validation: DepositPercent > 100 rejected
        var invalidRes = await _client.PutAsJsonAsync("/api/admin/system-settings",
            new UpdateSystemSettingsRequest(10, 60, 150m, false));
        Assert.Equal(HttpStatusCode.BadRequest, invalidRes.StatusCode);

        // 3. Validation: HoldDurationMinutes <= 0 rejected
        var invalidHoldRes = await _client.PutAsJsonAsync("/api/admin/system-settings",
            new UpdateSystemSettingsRequest(0, 60, 30m, false));
        Assert.Equal(HttpStatusCode.BadRequest, invalidHoldRes.StatusCode);

        // 4. Update DefaultDepositPercent to 50% and HoldDuration to 15 min
        var updateRes = await _client.PutAsJsonAsync("/api/admin/system-settings",
            new UpdateSystemSettingsRequest(15, 30, 50.00m, true));
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        var updated = await updateRes.Content.ReadFromJsonAsync<SystemSettingsDto>();
        Assert.NotNull(updated);
        Assert.Equal(50.00m, updated.DefaultDepositPercent);
        Assert.Equal(15, updated.HoldDurationMinutes);

        // 5. Regression: New Booking Hold calculates 50% deposit
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)).ToString("yyyy-MM-dd");
        var availRes = await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date={targetDate}");
        var avail = await availRes.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        Assert.NotNull(avail);
        var slot = avail.Slots.First(s => s.Status == AvailabilitySlotStatus.Available);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var holdRes = await _client.PostAsJsonAsync("/api/bookings/hold",
            new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { slot.StartAt }));
        Assert.Equal(HttpStatusCode.Created, holdRes.StatusCode);
        var hold = await holdRes.Content.ReadFromJsonAsync<BookingHoldResponse>();
        Assert.NotNull(hold);
        Assert.Equal(50.00m, hold.DepositPercent);
        Assert.Equal(hold.TotalAmount * 0.5m, hold.DepositAmount);

        // Verify hold expiration is 15 minutes ahead
        var duration = hold.HoldExpiresAt - DateTimeOffset.UtcNow;
        Assert.True(duration.TotalMinutes > 14 && duration.TotalMinutes <= 15.5);
    }

    [Fact]
    public async Task CancellationPolicy_Versioning_SingleActive_And_Refund_Regression()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // 1. Overlapping policy rules rejected
        var invalidRulesRes = await _client.PostAsJsonAsync("/api/admin/cancellation-policies",
            new CreateCancellationPolicyRequest("Faulty Policy", null, new[]
            {
                new CreateCancellationPolicyRuleRequest(0, 24, 0m),
                new CreateCancellationPolicyRuleRequest(12, 48, 50m) // overlaps with 0-24
            }));
        Assert.Equal(HttpStatusCode.BadRequest, invalidRulesRes.StatusCode);

        // 2. Create Policy Version 2 (Strict Policy: 0-12h 0%, 12-24h 50%, >=24h 80%)
        var createRes = await _client.PostAsJsonAsync("/api/admin/cancellation-policies",
            new CreateCancellationPolicyRequest("Strict Policy v2", null, new[]
            {
                new CreateCancellationPolicyRuleRequest(0, 12, 0m),
                new CreateCancellationPolicyRuleRequest(12, 24, 50.00m),
                new CreateCancellationPolicyRuleRequest(24, null, 80.00m)
            }));
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var policyV2 = await createRes.Content.ReadFromJsonAsync<AdminCancellationPolicyDetailDto>();
        Assert.NotNull(policyV2);
        Assert.False(policyV2.IsActive);

        // 3. Activate Policy Version 2
        var activateRes = await _client.PostAsync($"/api/admin/cancellation-policies/{policyV2.Id}/activate", null);
        Assert.Equal(HttpStatusCode.OK, activateRes.StatusCode);
        var activatedV2 = await activateRes.Content.ReadFromJsonAsync<AdminCancellationPolicyDetailDto>();
        Assert.NotNull(activatedV2);
        Assert.True(activatedV2.IsActive);

        // 4. Verify only one active policy exists in the database
        using var db = _factory.CreateDbContext();
        var activeCount = await db.CancellationPolicies.CountAsync(p => p.IsActive);
        Assert.Equal(1, activeCount);

        // 5. New Booking Hold automatically snapshots the newly active policy ID
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)).ToString("yyyy-MM-dd");
        var availRes = await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date={targetDate}");
        var avail = await availRes.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        var slot = avail!.Slots.First(s => s.Status == AvailabilitySlotStatus.Available);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var holdRes = await _client.PostAsJsonAsync("/api/bookings/hold",
            new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { slot.StartAt }));
        Assert.Equal(HttpStatusCode.Created, holdRes.StatusCode);

        var booking = await db.Bookings.OrderByDescending(b => b.CreatedAt).FirstAsync();
        Assert.Equal(policyV2.Id, booking.CancellationPolicyId);
    }
}
