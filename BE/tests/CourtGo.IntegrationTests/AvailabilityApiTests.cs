using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CourtGo.Application.Availability;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CourtGo.IntegrationTests;

public class AvailabilityApiTests : IClassFixture<AvailabilityApiTests.Factory>
{
    private readonly HttpClient _client;
    private readonly Factory _factory;

    public AvailabilityApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public class Factory : WebApplicationFactory<Program>
    {
        public Guid ActiveCourtId { get; } = Guid.Parse("99999999-9999-9999-9999-999999999999");
        public Guid CenterId { get; } = Guid.Parse("88888888-8888-8888-8888-888888888888");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(AppContext.BaseDirectory);
            builder.UseSetting("Jwt:Key", new string('x', 48));
            builder.ConfigureServices(services =>
            {
                var dbName = "AvailabilityIntegrationDb_" + Guid.NewGuid();
                var options = new DbContextOptionsBuilder<CourtGoDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options;

                var db = new CourtGoDbContext(options);
                SeedDatabase(db);

                // Replace DbContext in container
                foreach (var d in services.Where(d => d.ServiceType == typeof(CourtGoDbContext) || d.ServiceType == typeof(DbContextOptions<CourtGoDbContext>)).ToList())
                {
                    services.Remove(d);
                }

                services.AddSingleton(db);
            });
        }

        private void SeedDatabase(CourtGoDbContext db)
        {
            var sport = new Sport
            {
                Id = Guid.NewGuid(),
                Code = "badminton",
                Name = "Cầu lông",
                IsActive = true
            };

            var center = new SportCenter
            {
                Id = CenterId,
                Name = "CourtGo Arena Q7",
                AddressLine = "123 Huynh Tan Phat",
                District = "District 7",
                City = "Ho Chi Minh",
                TimeZoneId = "Asia/Ho_Chi_Minh",
                Status = SportCenterStatus.Active
            };

            var court = new Court
            {
                Id = ActiveCourtId,
                SportCenterId = center.Id,
                SportCenter = center,
                SportId = sport.Id,
                Sport = sport,
                Code = "A1",
                Name = "Sân A1",
                BasePricePerHour = 100000m,
                Status = CourtStatus.Active
            };

            // Every day open from 08:00 to 12:00 (4 slots: 08-09, 09-10, 10-11, 11-12)
            for (byte d = 1; d <= 7; d++)
            {
                db.OperatingHours.Add(new OperatingHour
                {
                    SportCenterId = center.Id,
                    DayOfWeek = (CourtGoDayOfWeek)d,
                    OpenTime = new TimeOnly(8, 0),
                    CloseTime = new TimeOnly(12, 0),
                    IsClosed = false
                });
            }

            db.SystemSettings.Add(new SystemSetting
            {
                Id = 1,
                MinBookingLeadMinutes = 30
            });

            db.Sports.Add(sport);
            db.SportCenters.Add(center);
            db.Courts.Add(court);
            db.SaveChanges();
        }
    }

    [Fact]
    public async Task GetAvailability_PrimaryEndpoint_Returns200_WithoutToken()
    {
        // Future date: 2027-01-15 (Friday)
        var res = await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date=2027-01-15");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var availability = await res.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        Assert.NotNull(availability);
        Assert.Equal("2027-01-15", availability.Date);
        Assert.False(availability.IsClosed);
        Assert.NotNull(availability.OpeningHours);
        Assert.Equal("08:00", availability.OpeningHours.OpenTime);
        Assert.Equal("12:00", availability.OpeningHours.CloseTime);
        Assert.Equal(4, availability.Slots.Count);
        Assert.All(availability.Slots, s => Assert.Equal(AvailabilitySlotStatus.Available, s.Status));
        Assert.All(availability.Slots, s => Assert.Equal(100000m, s.Price));
    }

    [Fact]
    public async Task GetAvailability_QueryStringAlias_Returns200()
    {
        var res = await _client.GetAsync($"/api/availability?courtId={_factory.ActiveCourtId}&date=2027-01-15");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var availability = await res.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        Assert.NotNull(availability);
        Assert.Equal(4, availability.Slots.Count);
    }

    [Fact]
    public async Task GetAvailability_PastDate_Returns400_With_InvalidDate_Code()
    {
        var res = await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date=2020-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INVALID_AVAILABILITY_DATE", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetAvailability_UnknownCourt_Returns404_With_CourtNotFound_Code()
    {
        var unknownId = Guid.NewGuid();
        var res = await _client.GetAsync($"/api/courts/{unknownId}/availability?date=2027-01-15");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("COURT_NOT_FOUND", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AvailabilityPing_Returns200()
    {
        var res = await _client.GetAsync("/api/availability/ping");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Swagger_Contains_CourtAvailability_Endpoint()
    {
        var res = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var json = await res.Content.ReadAsStringAsync();
        Assert.Contains("/api/courts/{courtId}/availability", json);
    }

    [Fact]
    public async Task GetAvailability_With_CourtBlock_And_Booking_Reflects_Slot_Statuses()
    {
        // 2027-02-01 (Monday)
        var date = new DateOnly(2027, 2, 1);
        var offset = TimeSpan.FromHours(7);

        // Add a block on 08-09 and a booking on 09-10
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();

            db.CourtBlocks.Add(new CourtBlock
            {
                CourtId = _factory.ActiveCourtId,
                StartAt = new DateTimeOffset(2027, 2, 1, 8, 0, 0, offset),
                EndAt = new DateTimeOffset(2027, 2, 1, 9, 0, 0, offset),
                Reason = "Maintenance"
            });

            db.BookingSlots.Add(new BookingSlot
            {
                CourtId = _factory.ActiveCourtId,
                StartAt = new DateTimeOffset(2027, 2, 1, 9, 0, 0, offset),
                EndAt = new DateTimeOffset(2027, 2, 1, 10, 0, 0, offset),
                ReservationState = ReservationState.Reserved,
                IsOccupying = true
            });

            // Add closed exception on 2027-02-02
            db.OperatingHourExceptions.Add(new OperatingHourException
            {
                SportCenterId = _factory.CenterId,
                Date = new DateOnly(2027, 2, 2),
                IsClosed = true
            });

            await db.SaveChangesAsync();
        }

        // Test active date with block and booking
        var res = await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date=2027-02-01");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var data = await res.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        Assert.NotNull(data);
        Assert.Equal(4, data.Slots.Count);
        Assert.Equal(AvailabilitySlotStatus.Blocked, data.Slots[0].Status); // 08-09 Blocked
        Assert.Equal(AvailabilitySlotStatus.Booked, data.Slots[1].Status);  // 09-10 Booked
        Assert.Equal(AvailabilitySlotStatus.Available, data.Slots[2].Status); // 10-11 Available
        Assert.Equal(AvailabilitySlotStatus.Available, data.Slots[3].Status); // 11-12 Available

        // Test closed date
        var closedRes = await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date=2027-02-02");
        Assert.Equal(HttpStatusCode.OK, closedRes.StatusCode);
        var closedData = await closedRes.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        Assert.NotNull(closedData);
        Assert.True(closedData.IsClosed);
        Assert.Empty(closedData.Slots);
    }
}
