using System.Net;
using System.Net.Http.Json;
using CourtGo.Application.Courts;
using CourtGo.Application.Interfaces;
using CourtGo.Application.SportCenters;
using CourtGo.Application.Sports;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CourtGo.IntegrationTests;

public class MasterDataApiTests : IClassFixture<MasterDataApiTests.Factory>
{
    private readonly HttpClient _client;
    private readonly Factory _factory;

    public MasterDataApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public class Factory : WebApplicationFactory<Program>
    {
        public MemorySportRepo SportsRepo { get; } = new();
        public MemorySportCenterRepo CentersRepo { get; } = new();
        public MemoryCourtRepo CourtsRepo { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(AppContext.BaseDirectory);
            builder.UseSetting("Jwt:Key", new string('x', 48));
            builder.ConfigureServices(services =>
            {
                SeedData();

                Replace<ISportRepository>(services, SportsRepo);
                Replace<ISportCenterRepository>(services, CentersRepo);
                Replace<ICourtRepository>(services, CourtsRepo);
            });
        }

        private void SeedData()
        {
            var badminton = new Sport
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Code = "badminton",
                Name = "Cầu lông",
                IconUrl = "🏸",
                DisplayOrder = 1,
                IsActive = true
            };

            var football = new Sport
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Code = "football",
                Name = "Bóng đá",
                IconUrl = "⚽",
                DisplayOrder = 2,
                IsActive = true
            };

            SportsRepo.Sports.Add(badminton);
            SportsRepo.Sports.Add(football);

            var center1 = new SportCenter
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Name = "CourtGo Sports Arena",
                AddressLine = "32 Huynh Tan Phat",
                District = "District 7",
                City = "Ho Chi Minh",
                Status = SportCenterStatus.Active
            };

            var court1 = new Court
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                SportCenterId = center1.Id,
                SportCenter = center1,
                SportId = badminton.Id,
                Sport = badminton,
                Code = "A1",
                Name = "Court A1",
                BasePricePerHour = 100000,
                Status = CourtStatus.Active
            };

            center1.Courts.Add(court1);
            CentersRepo.Centers.Add(center1);
            CourtsRepo.Courts.Add(court1);
        }

        private static void Replace<T>(IServiceCollection services, T instance) where T : class
        {
            foreach (var d in services.Where(d => d.ServiceType == typeof(T)).ToList()) services.Remove(d);
            services.AddSingleton(instance);
        }
    }

    public class MemorySportRepo : ISportRepository
    {
        public List<Sport> Sports { get; } = new();

        public Task<IReadOnlyList<Sport>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
        {
            var q = Sports.AsEnumerable();
            if (activeOnly) q = q.Where(s => s.IsActive);
            return Task.FromResult<IReadOnlyList<Sport>>(q.OrderBy(s => s.DisplayOrder).ToList());
        }

        public Task<Sport?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Sports.FirstOrDefault(s => s.Id == id));
    }

    public class MemorySportCenterRepo : ISportCenterRepository
    {
        public List<SportCenter> Centers { get; } = new();

        public Task<IReadOnlyList<SportCenter>> GetAllAsync(
            Guid? sportId = null,
            string? city = null,
            string? district = null,
            string? search = null,
            CancellationToken ct = default)
        {
            var q = Centers.Where(c => c.Status == SportCenterStatus.Active);
            if (sportId.HasValue) q = q.Where(c => c.Courts.Any(ct => ct.SportId == sportId.Value && ct.Status == CourtStatus.Active));
            if (!string.IsNullOrWhiteSpace(city)) q = q.Where(c => string.Equals(c.City, city.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(district)) q = q.Where(c => string.Equals(c.District, district.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(search)) q = q.Where(c => c.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<IReadOnlyList<SportCenter>>(q.ToList());
        }

        public Task<SportCenter?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Centers.FirstOrDefault(c => c.Id == id));
    }

    public class MemoryCourtRepo : ICourtRepository
    {
        public List<Court> Courts { get; } = new();

        public Task<IReadOnlyList<Court>> GetAllAsync(
            Guid? sportCenterId = null,
            Guid? sportId = null,
            bool activeOnly = true,
            CancellationToken ct = default)
        {
            var q = Courts.AsEnumerable();
            if (activeOnly) q = q.Where(c => c.Status == CourtStatus.Active);
            if (sportCenterId.HasValue) q = q.Where(c => c.SportCenterId == sportCenterId.Value);
            if (sportId.HasValue) q = q.Where(c => c.SportId == sportId.Value);
            return Task.FromResult<IReadOnlyList<Court>>(q.ToList());
        }

        public Task<Court?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Courts.FirstOrDefault(c => c.Id == id));
    }

    [Fact]
    public async Task GetSports_Returns200_WithSportsList()
    {
        var res = await _client.GetAsync("/api/sports");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var sports = await res.Content.ReadFromJsonAsync<List<SportDto>>();
        Assert.NotNull(sports);
        Assert.NotEmpty(sports);
        Assert.Contains(sports, s => s.Code == "badminton");
    }

    [Fact]
    public async Task GetSportById_Returns200_WhenExists()
    {
        var sportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var res = await _client.GetAsync($"/api/sports/{sportId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var sport = await res.Content.ReadFromJsonAsync<SportDto>();
        Assert.NotNull(sport);
        Assert.Equal("Cầu lông", sport.Name);
    }

    [Fact]
    public async Task GetSportById_Returns404_WhenNotFound()
    {
        var res = await _client.GetAsync($"/api/sports/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task GetSportCenters_Returns200_WithCalculatedFields()
    {
        var res = await _client.GetAsync("/api/sport-centers");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var centers = await res.Content.ReadFromJsonAsync<List<SportCenterSummaryDto>>();
        Assert.NotNull(centers);
        Assert.NotEmpty(centers);
        var center = centers[0];
        Assert.Equal("CourtGo Sports Arena", center.Name);
        Assert.Equal(100000, center.PriceFrom);
        Assert.Equal(1, center.TotalCourts);
        Assert.Contains("Cầu lông", center.Sports);
    }

    [Fact]
    public async Task GetSportCenterById_Returns200_WithDetails()
    {
        var centerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var res = await _client.GetAsync($"/api/sport-centers/{centerId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var detail = await res.Content.ReadFromJsonAsync<SportCenterDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal("CourtGo Sports Arena", detail.Name);
        Assert.NotEmpty(detail.Courts);
    }

    [Fact]
    public async Task GetSportCenterById_Returns404_WhenNotFound()
    {
        var res = await _client.GetAsync($"/api/sport-centers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task GetCourts_Returns200_WithCourtsList()
    {
        var centerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var res = await _client.GetAsync($"/api/courts?sportCenterId={centerId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var courts = await res.Content.ReadFromJsonAsync<List<CourtDto>>();
        Assert.NotNull(courts);
        Assert.NotEmpty(courts);
        Assert.Equal("Court A1", courts[0].Name);
    }

    [Fact]
    public async Task GetCourtById_Returns200_WhenExists()
    {
        var courtId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var res = await _client.GetAsync($"/api/courts/{courtId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var court = await res.Content.ReadFromJsonAsync<CourtDto>();
        Assert.NotNull(court);
        Assert.Equal("Court A1", court.Name);
    }

    [Fact]
    public async Task GetCourtById_Returns404_WhenNotFound()
    {
        var res = await _client.GetAsync($"/api/courts/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task PingEndpoints_Return200()
    {
        var resSports = await _client.GetAsync("/api/sports/ping");
        Assert.Equal(HttpStatusCode.OK, resSports.StatusCode);

        var resCenters = await _client.GetAsync("/api/sport-centers/ping");
        Assert.Equal(HttpStatusCode.OK, resCenters.StatusCode);

        var resCourts = await _client.GetAsync("/api/courts/ping");
        Assert.Equal(HttpStatusCode.OK, resCourts.StatusCode);
    }
}
