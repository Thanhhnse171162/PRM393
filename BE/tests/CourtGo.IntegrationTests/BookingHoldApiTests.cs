using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CourtGo.Application.Availability;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Auth;
using CourtGo.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CourtGo.IntegrationTests;

public class BookingHoldApiTests : IClassFixture<BookingHoldApiTests.Factory>
{
    private readonly HttpClient _client;
    private readonly Factory _factory;

    public BookingHoldApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public class Factory : WebApplicationFactory<Program>
    {
        public Guid ActiveCourtId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public Guid InactiveCourtId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public Guid CenterId { get; } = Guid.Parse("33333333-3333-3333-3333-333333333333");

        public User ActiveCustomer { get; private set; } = null!;
        public User SecondCustomer { get; private set; } = null!;
        public User InactiveCustomer { get; private set; } = null!;
        public User StaffUser { get; private set; } = null!;
        public User AdminUser { get; private set; } = null!;

        public string ActiveCustomerToken => GetToken(ActiveCustomer);
        public string SecondCustomerToken => GetToken(SecondCustomer);
        public string InactiveCustomerToken => GetToken(InactiveCustomer);
        public string StaffToken => GetToken(StaffUser);
        public string AdminToken => GetToken(AdminUser);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(AppContext.BaseDirectory);
            var jwtKey = new string('x', 48);
            builder.UseSetting("Jwt:Key", jwtKey);
            builder.UseSetting("Workers:Enabled", "false");

            builder.ConfigureServices(services =>
            {
                var dbName = "BookingHoldIntegrationDb_" + Guid.NewGuid();

                // Replace DbContext in DI with scoped in-memory database
                foreach (var d in services.Where(d => d.ServiceType == typeof(CourtGoDbContext) || d.ServiceType == typeof(DbContextOptions<CourtGoDbContext>)).ToList())
                {
                    services.Remove(d);
                }
                services.AddDbContext<CourtGoDbContext>(opts => opts.UseInMemoryDatabase(dbName));

                // Seed database using temporary provider
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
                SeedDatabase(db);
            });
        }

        public CourtGoDbContext CreateDbContext() =>
            Services.CreateScope().ServiceProvider.GetRequiredService<CourtGoDbContext>();

        public string GetToken(User user)
        {
            var jwt = Services.GetRequiredService<IJwtTokenService>();
            return jwt.CreateAccessToken(user, DateTimeOffset.UtcNow).Token;
        }

        private void SeedDatabase(CourtGoDbContext db)
        {
            var hasher = new PasswordHasher();

            ActiveCustomer = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Customer One",
                PhoneNumber = "0901111111",
                Email = "customer1@courtgo.vn",
                PasswordHash = hasher.Hash("Demo@123456"),
                Role = UserRole.Customer,
                IsActive = true
            };
            SecondCustomer = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Customer Two",
                PhoneNumber = "0902222222",
                Email = "customer2@courtgo.vn",
                PasswordHash = hasher.Hash("Demo@123456"),
                Role = UserRole.Customer,
                IsActive = true
            };
            InactiveCustomer = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Inactive Customer",
                PhoneNumber = "0909999999",
                Email = "inactive@courtgo.vn",
                PasswordHash = hasher.Hash("Demo@123456"),
                Role = UserRole.Customer,
                IsActive = false
            };
            StaffUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Staff Member",
                PhoneNumber = "0903333333",
                Email = "staff@courtgo.vn",
                PasswordHash = hasher.Hash("Demo@123456"),
                Role = UserRole.Staff,
                IsActive = true
            };
            AdminUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Admin Member",
                PhoneNumber = "0904444444",
                Email = "admin@courtgo.vn",
                PasswordHash = hasher.Hash("Demo@123456"),
                Role = UserRole.Admin,
                IsActive = true
            };

            db.Users.AddRange(ActiveCustomer, SecondCustomer, InactiveCustomer, StaffUser, AdminUser);

            var sport = new Sport
            {
                Id = Guid.NewGuid(),
                Code = "badminton",
                Name = "Cầu lông",
                IsActive = true
            };
            db.Sports.Add(sport);

            var center = new SportCenter
            {
                Id = CenterId,
                Name = "CourtGo Center Q7",
                AddressLine = "456 Nguyen Thi Thap",
                District = "District 7",
                City = "Ho Chi Minh",
                TimeZoneId = "Asia/Ho_Chi_Minh",
                Status = SportCenterStatus.Active
            };
            db.SportCenters.Add(center);

            var activeCourt = new Court
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
            var inactiveCourt = new Court
            {
                Id = InactiveCourtId,
                SportCenterId = center.Id,
                SportCenter = center,
                SportId = sport.Id,
                Sport = sport,
                Code = "A2",
                Name = "Sân A2 (Bảo trì)",
                BasePricePerHour = 100000m,
                Status = CourtStatus.Inactive
            };
            db.Courts.AddRange(activeCourt, inactiveCourt);

            // Open 06:00 to 22:00 every day
            for (byte d = 1; d <= 7; d++)
            {
                db.OperatingHours.Add(new OperatingHour
                {
                    SportCenterId = center.Id,
                    DayOfWeek = (CourtGoDayOfWeek)d,
                    OpenTime = new TimeOnly(6, 0),
                    CloseTime = new TimeOnly(22, 0),
                    IsClosed = false
                });
            }

            db.SystemSettings.Add(new SystemSetting
            {
                Id = 1,
                HoldDurationMinutes = 10,
                MinBookingLeadMinutes = 30,
                DefaultDepositPercent = 30.00m
            });

            db.SaveChanges();
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string? token = null, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(token))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        if (body != null)
        {
            req.Content = JsonContent.Create(body);
        }
        return req;
    }

    [Fact]
    public async Task Hold_Unauthenticated_Returns401()
    {
        var slot = DateTimeOffset.UtcNow.AddDays(2).Date.AddHours(10);
        var body = new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { slot });
        var req = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: null, body: body);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Hold_StaffToken_Returns403()
    {
        var slot = DateTimeOffset.UtcNow.AddDays(2).Date.AddHours(10);
        var body = new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { slot });
        var req = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: _factory.StaffToken, body: body);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Hold_AdminToken_Returns403()
    {
        var slot = DateTimeOffset.UtcNow.AddDays(2).Date.AddHours(10);
        var body = new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { slot });
        var req = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: _factory.AdminToken, body: body);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Hold_InactiveCustomer_Returns403_AccountInactive()
    {
        var slot = DateTimeOffset.UtcNow.AddDays(2).Date.AddHours(10);
        var body = new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { slot });
        var req = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: _factory.InactiveCustomerToken, body: body);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);

        var prob = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(prob);
        Assert.Equal(ErrorCodes.AccountInactive, prob.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Hold_EmptySlots_Returns400_InvalidSlotSelection()
    {
        var body = new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset>());
        var req = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: _factory.ActiveCustomerToken, body: body);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var prob = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(prob);
        Assert.Equal(ErrorCodes.InvalidSlotSelection, prob.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Hold_DuplicateSlots_Returns400()
    {
        var slot = DateTimeOffset.UtcNow.AddDays(3).Date.AddHours(10);
        var body = new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { slot, slot });
        var req = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: _factory.ActiveCustomerToken, body: body);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Hold_NonConsecutiveSlots_Returns400()
    {
        var s1 = DateTimeOffset.UtcNow.AddDays(3).Date.AddHours(10);
        var s2 = DateTimeOffset.UtcNow.AddDays(3).Date.AddHours(12);
        var body = new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { s1, s2 });
        var req = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: _factory.ActiveCustomerToken, body: body);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Hold_DifferentCalendarDates_Returns400()
    {
        var s1 = DateTimeOffset.UtcNow.AddDays(3).Date.AddHours(23);
        var s2 = DateTimeOffset.UtcNow.AddDays(4).Date.AddHours(0);
        var body = new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { s1, s2 });
        var req = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: _factory.ActiveCustomerToken, body: body);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Hold_InvalidCourt_Returns404()
    {
        var slot = DateTimeOffset.UtcNow.AddDays(3).Date.AddHours(10);
        var body = new BookingHoldRequest(Guid.NewGuid(), new List<DateTimeOffset> { slot });
        var req = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: _factory.ActiveCustomerToken, body: body);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Hold_InactiveCourt_Returns409()
    {
        var slot = DateTimeOffset.UtcNow.AddDays(3).Date.AddHours(10);
        var body = new BookingHoldRequest(_factory.InactiveCourtId, new List<DateTimeOffset> { slot });
        var req = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: _factory.ActiveCustomerToken, body: body);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Hold_SuccessfulOneSlot_Returns201_AndCreatesHold()
    {
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var slotTime = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(14, 0)), TimeSpan.FromHours(7));
        var body = new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { slotTime });
        var req = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: _factory.ActiveCustomerToken, body: body);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var hold = await res.Content.ReadFromJsonAsync<BookingHoldResponse>();
        Assert.NotNull(hold);
        Assert.Equal("PendingPayment", hold.BookingStatus);
        Assert.Equal("Unpaid", hold.PaymentStatus);
        Assert.Single(hold.Slots);
        Assert.Equal(100000m, hold.TotalAmount);
        Assert.Equal(30000m, hold.DepositAmount);
        Assert.Equal(70000m, hold.RemainingAmount);
        Assert.StartsWith("CG-", hold.BookingCode);

        // Verify DB state
        await using var db = _factory.CreateDbContext();
        var booking = await db.Bookings
            .Include(b => b.Slots)
            .Include(b => b.Payments)
            .FirstAsync(b => b.Id == hold.BookingId);

        Assert.Null(booking.QrToken);
        Assert.Empty(booking.Payments);
        Assert.Single(booking.Slots);
        var slot = booking.Slots.First();
        Assert.Equal(ReservationState.Held, slot.ReservationState);
        Assert.True(slot.IsOccupying);
        Assert.Equal(booking.HoldExpiresAt, slot.HoldExpiresAt);

        // Verify Availability returns Held
        var availRes = await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date={targetDate:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, availRes.StatusCode);
        var avail = await availRes.Content.ReadFromJsonAsync<CourtAvailabilityDto>();
        Assert.NotNull(avail);
        var matchedSlot = avail.Slots.FirstOrDefault(s => s.StartAt == slotTime);
        Assert.NotNull(matchedSlot);
        Assert.Equal(AvailabilitySlotStatus.Held, matchedSlot.Status);
    }

    [Fact]
    public async Task Hold_ConcurrentCustomers_SameSlot_ExactlyOneSucceeds_OneFails409()
    {
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6));
        var slotTime = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(18, 0)), TimeSpan.FromHours(7));

        var bodyA = new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { slotTime });
        var bodyB = new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { slotTime });

        var reqA = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: _factory.ActiveCustomerToken, body: bodyA);
        var reqB = CreateRequest(HttpMethod.Post, "/api/bookings/hold", token: _factory.SecondCustomerToken, body: bodyB);

        // Send both requests concurrently
        var taskA = _client.SendAsync(reqA);
        var taskB = _client.SendAsync(reqB);

        var responses = await Task.WhenAll(taskA, taskB);

        var statusCodes = responses.Select(r => r.StatusCode).ToList();
        Assert.Contains(HttpStatusCode.Created, statusCodes);
        Assert.Contains(HttpStatusCode.Conflict, statusCodes);

        // Check error response of the failed request
        var failedRes = responses.First(r => r.StatusCode == HttpStatusCode.Conflict);
        var prob = await failedRes.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(prob);
        Assert.Equal(ErrorCodes.BookingSlotConflict, prob.Extensions["code"]?.ToString());

        // Verify in database: exactly ONE occupying booking slot exists for this slot
        await using var dbCheck = _factory.CreateDbContext();
        var occupyingCount = await dbCheck.BookingSlots
            .CountAsync(s => s.CourtId == _factory.ActiveCourtId && s.StartAt == slotTime && s.IsOccupying);
        Assert.Equal(1, occupyingCount);
    }

    [Fact]
    public async Task Hold_MultiSlotAtomicity_WhenOneSlotOccupied_RollsBackEverything()
    {
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var s1 = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(19, 0)), TimeSpan.FromHours(7));
        var s2 = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(20, 0)), TimeSpan.FromHours(7));

        // First customer holds 20:00 - 21:00
        var holdRes1 = await _client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/bookings/hold",
            token: _factory.ActiveCustomerToken,
            body: new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { s2 })));
        Assert.Equal(HttpStatusCode.Created, holdRes1.StatusCode);

        // Second customer attempts to hold 19:00 - 20:00 AND 20:00 - 21:00
        var holdRes2 = await _client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/bookings/hold",
            token: _factory.SecondCustomerToken,
            body: new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { s1, s2 })));
        Assert.Equal(HttpStatusCode.Conflict, holdRes2.StatusCode);

        // Second customer's attempt must not leave 19:00 - 20:00 occupied
        await using var dbRollback = _factory.CreateDbContext();
        var slot1Occupying = await dbRollback.BookingSlots
            .AnyAsync(s => s.CourtId == _factory.ActiveCourtId && s.StartAt == s1 && s.IsOccupying);
        Assert.False(slot1Occupying, "Failed multi-slot hold must not leave partial slots occupied!");
    }
}
