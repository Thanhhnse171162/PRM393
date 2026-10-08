using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CourtGo.Application.Availability;
using CourtGo.Application.Bookings;
using CourtGo.Application.Courts;
using CourtGo.Application.SportCenters;
using CourtGo.Application.Sports;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CourtGo.IntegrationTests;

public class AdminMasterDataApiTests : IDisposable
{
    private readonly BookingHoldApiTests.Factory _factory = new();
    private readonly HttpClient _client;

    public AdminMasterDataApiTests()
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
    public async Task AdminMasterDataEndpoints_RequireAdminRole(string role)
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

        var dummyId = Guid.NewGuid();

        var routes = new[]
        {
            _client.GetAsync("/api/admin/sport-centers"),
            _client.GetAsync($"/api/admin/sport-centers/{dummyId}"),
            _client.PostAsJsonAsync("/api/admin/sport-centers", new CreateSportCenterRequest("Name", "Addr", null, "Dist", "City")),
            _client.PutAsJsonAsync($"/api/admin/sport-centers/{dummyId}", new UpdateSportCenterRequest("Name", "Addr", null, "Dist", "City")),
            _client.PatchAsJsonAsync($"/api/admin/sport-centers/{dummyId}/status", new UpdateSportCenterStatusRequest(SportCenterStatus.Inactive)),

            _client.GetAsync("/api/admin/sports"),
            _client.GetAsync($"/api/admin/sports/{dummyId}"),
            _client.PostAsJsonAsync("/api/admin/sports", new CreateSportRequest("code", "Name")),
            _client.PutAsJsonAsync($"/api/admin/sports/{dummyId}", new UpdateSportRequest("Name")),
            _client.PatchAsJsonAsync($"/api/admin/sports/{dummyId}/status", new UpdateSportStatusRequest(false)),

            _client.GetAsync("/api/admin/courts"),
            _client.GetAsync($"/api/admin/courts/{dummyId}"),
            _client.PostAsJsonAsync("/api/admin/courts", new CreateCourtRequest(_factory.CenterId, dummyId, "C1", "Court 1")),
            _client.PutAsJsonAsync($"/api/admin/courts/{dummyId}", new UpdateCourtRequest("Court 1")),
            _client.PatchAsJsonAsync($"/api/admin/courts/{dummyId}/status", new UpdateCourtStatusRequest(CourtStatus.Maintenance))
        };

        var responses = await Task.WhenAll(routes);
        var expected = role == "none" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;

        foreach (var res in responses)
        {
            Assert.Equal(expected, res.StatusCode);
        }
    }

    [Fact]
    public async Task SportCenter_Lifecycle_List_Create_Validate_Update_Status_AndPublicRegression()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // 1. List centers with search
        var listRes = await _client.GetAsync("/api/admin/sport-centers?search=CourtGo");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var listData = await listRes.Content.ReadFromJsonAsync<PagedResult<AdminSportCenterListItemDto>>();
        Assert.NotNull(listData);
        Assert.NotEmpty(listData.Items);

        // 2. Reject invalid latitude
        var invalidLatRes = await _client.PostAsJsonAsync("/api/admin/sport-centers",
            new CreateSportCenterRequest("Invalid Lat Center", "123 Street", null, "District 1", "HCMC", Latitude: 120m));
        Assert.Equal(HttpStatusCode.BadRequest, invalidLatRes.StatusCode);

        // 3. Reject invalid longitude
        var invalidLngRes = await _client.PostAsJsonAsync("/api/admin/sport-centers",
            new CreateSportCenterRequest("Invalid Lng Center", "123 Street", null, "District 1", "HCMC", Longitude: 200m));
        Assert.Equal(HttpStatusCode.BadRequest, invalidLngRes.StatusCode);

        // 4. Reject invalid timezone
        var invalidTzRes = await _client.PostAsJsonAsync("/api/admin/sport-centers",
            new CreateSportCenterRequest("Invalid Tz Center", "123 Street", null, "District 1", "HCMC", TimeZoneId: "Invalid/Unrecognized_Timezone"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidTzRes.StatusCode);

        // 5. Create valid center
        var createRes = await _client.PostAsJsonAsync("/api/admin/sport-centers",
            new CreateSportCenterRequest("CourtGo Thu Duc", "100 Vo Van Ngan", "Linh Chieu", "Thu Duc", "Ho Chi Minh", 10.85m, 106.77m, "Asia/Ho_Chi_Minh", "0908123456"));
        Assert.Equal(HttpStatusCode.OK, createRes.StatusCode);
        var createdCenter = await createRes.Content.ReadFromJsonAsync<AdminSportCenterDetailDto>();
        Assert.NotNull(createdCenter);
        Assert.Equal("CourtGo Thu Duc", createdCenter.Name);
        Assert.Equal("Active", createdCenter.Status);

        // 6. Get detail
        var detailRes = await _client.GetAsync($"/api/admin/sport-centers/{createdCenter.CenterId}");
        Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);

        // 7. Update profile
        var updateRes = await _client.PutAsJsonAsync($"/api/admin/sport-centers/{createdCenter.CenterId}",
            new UpdateSportCenterRequest("CourtGo Thu Duc Premium", "100 Vo Van Ngan", "Linh Chieu", "Thu Duc", "Ho Chi Minh", 10.85m, 106.77m, "Asia/Ho_Chi_Minh", "0908123456"));
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        var updatedCenter = await updateRes.Content.ReadFromJsonAsync<AdminSportCenterDetailDto>();
        Assert.NotNull(updatedCenter);
        Assert.Equal("CourtGo Thu Duc Premium", updatedCenter.Name);

        // 8. Public customer can view active center
        var publicRes = await _client.GetAsync($"/api/sport-centers/{createdCenter.CenterId}");
        Assert.Equal(HttpStatusCode.OK, publicRes.StatusCode);

        // 9. Deactivate center
        var statusRes = await _client.PatchAsJsonAsync($"/api/admin/sport-centers/{createdCenter.CenterId}/status",
            new UpdateSportCenterStatusRequest(SportCenterStatus.Inactive));
        Assert.Equal(HttpStatusCode.OK, statusRes.StatusCode);

        // 10. Public customer browse hides inactive center
        var publicHiddenRes = await _client.GetAsync($"/api/sport-centers/{createdCenter.CenterId}");
        Assert.Equal(HttpStatusCode.NotFound, publicHiddenRes.StatusCode);

        // 11. Unknown center returns 404
        var unknownRes = await _client.GetAsync($"/api/admin/sport-centers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, unknownRes.StatusCode);
    }

    [Fact]
    public async Task Sport_Lifecycle_Create_Duplicate_Update_Status_AndPublicRegression()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        // 1. List sports
        var listRes = await _client.GetAsync("/api/admin/sports");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var sports = await listRes.Content.ReadFromJsonAsync<PagedResult<AdminSportDto>>();
        Assert.NotNull(sports);
        Assert.NotEmpty(sports.Items);

        // 2. Create sport
        var createRes = await _client.PostAsJsonAsync("/api/admin/sports",
            new CreateSportRequest("pickleball", "Pickleball", "🏓", 5));
        Assert.Equal(HttpStatusCode.OK, createRes.StatusCode);
        var created = await createRes.Content.ReadFromJsonAsync<AdminSportDetailDto>();
        Assert.NotNull(created);
        Assert.Equal("pickleball", created.Code);
        Assert.Equal("Pickleball", created.Name);
        Assert.True(created.IsActive);

        // 3. Duplicate code rejection
        var dupCodeRes = await _client.PostAsJsonAsync("/api/admin/sports",
            new CreateSportRequest("pickleball", "Pickleball 2"));
        Assert.Equal(HttpStatusCode.Conflict, dupCodeRes.StatusCode);

        // 4. Duplicate name rejection
        var dupNameRes = await _client.PostAsJsonAsync("/api/admin/sports",
            new CreateSportRequest("pb2", "Pickleball"));
        Assert.Equal(HttpStatusCode.Conflict, dupNameRes.StatusCode);

        // 5. Update sport
        var updateRes = await _client.PutAsJsonAsync($"/api/admin/sports/{created.Id}",
            new UpdateSportRequest("Pickleball Pro", "🏓", 10));
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        var updated = await updateRes.Content.ReadFromJsonAsync<AdminSportDetailDto>();
        Assert.NotNull(updated);
        Assert.Equal("Pickleball Pro", updated.Name);
        Assert.Equal(10, updated.DisplayOrder);

        // 6. Deactivate sport
        var deactRes = await _client.PatchAsJsonAsync($"/api/admin/sports/{created.Id}/status",
            new UpdateSportStatusRequest(false));
        Assert.Equal(HttpStatusCode.OK, deactRes.StatusCode);

        // 7. Public customer sports list excludes inactive
        var publicSportsRes = await _client.GetAsync("/api/sports");
        var activeSports = await publicSportsRes.Content.ReadFromJsonAsync<List<SportDto>>();
        Assert.NotNull(activeSports);
        Assert.DoesNotContain(activeSports, s => s.Id == created.Id);

        // 8. Unknown sport returns 404
        var unknownRes = await _client.GetAsync($"/api/admin/sports/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, unknownRes.StatusCode);
    }

    [Fact]
    public async Task Court_Lifecycle_Create_Validate_DuplicateCode_Maintenance_Inactive_Regression()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        using var db = _factory.CreateDbContext();
        var sport = await db.Sports.FirstAsync(s => s.IsActive);

        // 1. Invalid center rejected
        var invalidCenterRes = await _client.PostAsJsonAsync("/api/admin/courts",
            new CreateCourtRequest(Guid.NewGuid(), sport.Id, "T1", "Court T1", BasePricePerHour: 150000));
        Assert.Equal(HttpStatusCode.NotFound, invalidCenterRes.StatusCode);

        // 2. Invalid sport rejected
        var invalidSportRes = await _client.PostAsJsonAsync("/api/admin/courts",
            new CreateCourtRequest(_factory.CenterId, Guid.NewGuid(), "T1", "Court T1", BasePricePerHour: 150000));
        Assert.Equal(HttpStatusCode.NotFound, invalidSportRes.StatusCode);

        // 3. Negative base price rejected
        var negPriceRes = await _client.PostAsJsonAsync("/api/admin/courts",
            new CreateCourtRequest(_factory.CenterId, sport.Id, "T1", "Court T1", BasePricePerHour: -50000));
        Assert.Equal(HttpStatusCode.BadRequest, negPriceRes.StatusCode);

        // 4. Create valid court
        var createRes = await _client.PostAsJsonAsync("/api/admin/courts",
            new CreateCourtRequest(_factory.CenterId, sport.Id, "K1", "Court K1", "Hard Court", "Pro badminton court", null, 150000));
        Assert.Equal(HttpStatusCode.OK, createRes.StatusCode);
        var court = await createRes.Content.ReadFromJsonAsync<AdminCourtDetailDto>();
        Assert.NotNull(court);
        Assert.Equal("K1", court.Code);
        Assert.Equal(150000, court.BasePricePerHour);
        Assert.Equal("Active", court.Status);

        // 5. Duplicate court code in same center rejected
        var dupRes = await _client.PostAsJsonAsync("/api/admin/courts",
            new CreateCourtRequest(_factory.CenterId, sport.Id, "K1", "Another Court K1", BasePricePerHour: 150000));
        Assert.Equal(HttpStatusCode.Conflict, dupRes.StatusCode);

        // 6. Update court profile
        var updateRes = await _client.PutAsJsonAsync($"/api/admin/courts/{court.CourtId}",
            new UpdateCourtRequest("Court K1 Premium", "Wood", "Upgraded surface", null, 180000));
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        var updatedCourt = await updateRes.Content.ReadFromJsonAsync<AdminCourtDetailDto>();
        Assert.NotNull(updatedCourt);
        Assert.Equal("Court K1 Premium", updatedCourt.Name);
        Assert.Equal(180000, updatedCourt.BasePricePerHour);

        // 7. Status -> Maintenance
        var maintRes = await _client.PatchAsJsonAsync($"/api/admin/courts/{court.CourtId}/status",
            new UpdateCourtStatusRequest(CourtStatus.Maintenance));
        Assert.Equal(HttpStatusCode.OK, maintRes.StatusCode);

        // Public customer availability returns 0 slots for Maintenance court
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd");
        var availRes = await _client.GetAsync($"/api/courts/{court.CourtId}/availability?date={date}");
        Assert.Equal(HttpStatusCode.OK, availRes.StatusCode);
        var avail = await availRes.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        Assert.NotNull(avail);
        Assert.Empty(avail.Slots);

        // 8. Status -> Inactive
        var inactRes = await _client.PatchAsJsonAsync($"/api/admin/courts/{court.CourtId}/status",
            new UpdateCourtStatusRequest(CourtStatus.Inactive));
        Assert.Equal(HttpStatusCode.OK, inactRes.StatusCode);

        // Public customer browse hides inactive court
        var publicCourtRes = await _client.GetAsync($"/api/courts/{court.CourtId}");
        Assert.Equal(HttpStatusCode.NotFound, publicCourtRes.StatusCode);

        // 9. Unknown court returns 404
        var unknownRes = await _client.GetAsync($"/api/admin/courts/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, unknownRes.StatusCode);
    }

    [Fact]
    public async Task HistoricalSnapshotProtection_RemainsIntact_AfterAdminRenames()
    {
        using var db = _factory.CreateDbContext();
        var center = await db.SportCenters.FirstAsync(c => c.Id == _factory.CenterId);
        var court = await db.Courts.FirstAsync(c => c.Id == _factory.ActiveCourtId);
        var sport = await db.Sports.FirstAsync(s => s.Id == court.SportId);

        var originalCenterName = center.Name;
        var originalCourtName = court.Name;
        var originalSportName = sport.Name;
        var originalUnitPrice = court.BasePricePerHour;

        // Create historical booking
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingCode = "BK-HIST-01",
            CustomerUserId = _factory.ActiveCustomer.Id,
            CourtId = court.Id,
            CenterNameSnapshot = originalCenterName,
            CourtNameSnapshot = originalCourtName,
            SportNameSnapshot = originalSportName,
            CustomerNameSnapshot = _factory.ActiveCustomer.FullName,
            CustomerPhoneSnapshot = _factory.ActiveCustomer.PhoneNumber,
            BookingStatus = BookingStatus.Confirmed,
            PaymentStatus = BookingPaymentStatus.FullyPaid,
            TotalAmount = originalUnitPrice * 2,
            DepositAmount = originalUnitPrice,
            StartAt = DateTimeOffset.UtcNow.AddHours(2),
            EndAt = DateTimeOffset.UtcNow.AddHours(4),
            DurationMinutes = 120,
            CreatedAt = DateTimeOffset.UtcNow
        };

        booking.Slots.Add(new BookingSlot
        {
            Id = Guid.NewGuid(),
            CourtId = court.Id,
            BookingId = booking.Id,
            StartAt = booking.StartAt,
            EndAt = booking.StartAt.AddHours(1),
            UnitPrice = originalUnitPrice
        });

        booking.Slots.Add(new BookingSlot
        {
            Id = Guid.NewGuid(),
            CourtId = court.Id,
            BookingId = booking.Id,
            StartAt = booking.StartAt.AddHours(1),
            EndAt = booking.EndAt,
            UnitPrice = originalUnitPrice
        });

        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        // Admin renames Center, Court, Sport and updates Court Price
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.AdminToken);

        await _client.PutAsJsonAsync($"/api/admin/sport-centers/{center.Id}",
            new UpdateSportCenterRequest("RENAMED Center", center.AddressLine, center.Ward, center.District, center.City, PhoneNumber: center.PhoneNumber));

        await _client.PutAsJsonAsync($"/api/admin/sports/{sport.Id}",
            new UpdateSportRequest("RENAMED Sport"));

        await _client.PutAsJsonAsync($"/api/admin/courts/{court.Id}",
            new UpdateCourtRequest("RENAMED Court", BasePricePerHour: 999999));

        // Customer views historical booking detail
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var bookingRes = await _client.GetAsync($"/api/bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.OK, bookingRes.StatusCode);

        var bookingDetail = await bookingRes.Content.ReadFromJsonAsync<BookingDetailDto>();
        Assert.NotNull(bookingDetail);

        // Historical snapshots must NOT have changed!
        Assert.Equal(originalCenterName, bookingDetail.Center.Name);
        Assert.Equal(originalCourtName, bookingDetail.Court.Name);
        Assert.Equal(originalSportName, bookingDetail.Sport.Name);

        // Unit prices in historical slots must NOT have changed!
        foreach (var slot in bookingDetail.Slots)
        {
            Assert.Equal(originalUnitPrice, slot.Price);
        }
    }
}
