using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Courts;
using CourtGo.Application.Interfaces;
using CourtGo.Application.SportCenters;
using CourtGo.Application.Sports;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;

namespace CourtGo.UnitTests;

public class MasterDataServiceTests
{
    private class FakeSportRepository : ISportRepository
    {
        public List<Sport> Sports { get; } = new();

        public Task<IReadOnlyList<Sport>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
        {
            var result = Sports.AsEnumerable();
            if (activeOnly) result = result.Where(s => s.IsActive);
            return Task.FromResult<IReadOnlyList<Sport>>(result.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToList());
        }

        public Task<Sport?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Sports.FirstOrDefault(s => s.Id == id));
    }

    private class FakeSportCenterRepository : ISportCenterRepository
    {
        public List<SportCenter> Centers { get; } = new();

        public Task<IReadOnlyList<SportCenter>> GetAllAsync(
            Guid? sportId = null,
            string? city = null,
            string? district = null,
            string? search = null,
            CancellationToken ct = default)
        {
            var query = Centers.Where(c => c.Status == SportCenterStatus.Active);

            if (sportId.HasValue)
            {
                query = query.Where(c => c.Courts.Any(ct => ct.SportId == sportId.Value && ct.Status == CourtStatus.Active));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(c => string.Equals(c.City, city.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(district))
            {
                query = query.Where(c => string.Equals(c.District, district.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(c => c.Name.Contains(s, StringComparison.OrdinalIgnoreCase) || c.AddressLine.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult<IReadOnlyList<SportCenter>>(query.OrderBy(c => c.Name).ToList());
        }

        public Task<SportCenter?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Centers.FirstOrDefault(c => c.Id == id));
    }

    private class FakeCourtRepository : ICourtRepository
    {
        public List<Court> Courts { get; } = new();

        public Task<IReadOnlyList<Court>> GetAllAsync(
            Guid? sportCenterId = null,
            Guid? sportId = null,
            bool activeOnly = true,
            CancellationToken ct = default)
        {
            var query = Courts.AsEnumerable();
            if (activeOnly) query = query.Where(c => c.Status == CourtStatus.Active);
            if (sportCenterId.HasValue) query = query.Where(c => c.SportCenterId == sportCenterId.Value);
            if (sportId.HasValue) query = query.Where(c => c.SportId == sportId.Value);

            return Task.FromResult<IReadOnlyList<Court>>(query.OrderBy(c => c.Name).ToList());
        }

        public Task<Court?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Courts.FirstOrDefault(c => c.Id == id));
    }

    [Fact]
    public async Task SportsService_GetAllAsync_ReturnsActiveSports_OrderedByDisplayOrder()
    {
        var repo = new FakeSportRepository();
        repo.Sports.AddRange(new[]
        {
            new Sport { Id = Guid.NewGuid(), Code = "football", Name = "Bóng đá", DisplayOrder = 2, IsActive = true },
            new Sport { Id = Guid.NewGuid(), Code = "badminton", Name = "Cầu lông", DisplayOrder = 1, IsActive = true },
            new Sport { Id = Guid.NewGuid(), Code = "inactive", Name = "Inactive Sport", DisplayOrder = 0, IsActive = false }
        });

        var service = new SportService(repo);
        var result = await service.GetAllAsync(activeOnly: true);

        Assert.Equal(2, result.Count);
        Assert.Equal("badminton", result[0].Code);
        Assert.Equal("football", result[1].Code);
    }

    [Fact]
    public async Task SportsService_GetByIdAsync_ThrowsNotFound_WhenMissing()
    {
        var repo = new FakeSportRepository();
        var service = new SportService(repo);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SportCenterService_GetAllAsync_CalculatesSportsAndPriceFrom()
    {
        var sport1 = new Sport { Id = Guid.NewGuid(), Name = "Cầu lông" };
        var sport2 = new Sport { Id = Guid.NewGuid(), Name = "Pickleball" };

        var center = new SportCenter
        {
            Id = Guid.NewGuid(),
            Name = "CourtGo Arena",
            AddressLine = "123 Main St",
            District = "District 7",
            City = "Ho Chi Minh",
            Status = SportCenterStatus.Active
        };

        var court1 = new Court
        {
            Id = Guid.NewGuid(),
            SportCenterId = center.Id,
            SportId = sport1.Id,
            Sport = sport1,
            BasePricePerHour = 120000,
            Status = CourtStatus.Active
        };

        var court2 = new Court
        {
            Id = Guid.NewGuid(),
            SportCenterId = center.Id,
            SportId = sport2.Id,
            Sport = sport2,
            BasePricePerHour = 150000,
            Status = CourtStatus.Active
        };

        center.Courts.Add(court1);
        center.Courts.Add(court2);

        var repo = new FakeSportCenterRepository();
        repo.Centers.Add(center);

        var service = new SportCenterService(repo);
        var result = await service.GetAllAsync();

        Assert.Single(result);
        var dto = result[0];
        Assert.Equal("CourtGo Arena", dto.Name);
        Assert.Equal(120000, dto.PriceFrom);
        Assert.Equal(2, dto.TotalCourts);
        Assert.Contains("Cầu lông", dto.Sports);
        Assert.Contains("Pickleball", dto.Sports);
    }

    [Fact]
    public async Task SportCenterService_Filters_ByCity_And_Sport()
    {
        var sportId = Guid.NewGuid();
        var sport = new Sport { Id = sportId, Name = "Tennis" };

        var centerHcm = new SportCenter
        {
            Id = Guid.NewGuid(),
            Name = "Center HCM",
            AddressLine = "Address HCM",
            District = "District 1",
            City = "Ho Chi Minh",
            Status = SportCenterStatus.Active
        };
        centerHcm.Courts.Add(new Court { SportId = sportId, Sport = sport, Status = CourtStatus.Active, BasePricePerHour = 200000 });

        var centerHanoi = new SportCenter
        {
            Id = Guid.NewGuid(),
            Name = "Center HN",
            AddressLine = "Address HN",
            District = "Ba Dinh",
            City = "Hanoi",
            Status = SportCenterStatus.Active
        };

        var repo = new FakeSportCenterRepository();
        repo.Centers.Add(centerHcm);
        repo.Centers.Add(centerHanoi);

        var service = new SportCenterService(repo);

        var filteredCity = await service.GetAllAsync(city: "hanoi");
        Assert.Single(filteredCity);
        Assert.Equal("Center HN", filteredCity[0].Name);

        var filteredSport = await service.GetAllAsync(sportId: sportId);
        Assert.Single(filteredSport);
        Assert.Equal("Center HCM", filteredSport[0].Name);
    }

    [Fact]
    public async Task SportCenterService_GetByIdAsync_ThrowsNotFound_WhenInactiveOrMissing()
    {
        var repo = new FakeSportCenterRepository();
        repo.Centers.Add(new SportCenter
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Center",
            Status = SportCenterStatus.Inactive
        });

        var service = new SportCenterService(repo);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(repo.Centers[0].Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CourtService_GetAllAndGetById_WorkCorrectly()
    {
        var centerId = Guid.NewGuid();
        var sportId = Guid.NewGuid();
        var court = new Court
        {
            Id = Guid.NewGuid(),
            SportCenterId = centerId,
            SportCenter = new SportCenter { Id = centerId, Name = "Arena" },
            SportId = sportId,
            Sport = new Sport { Id = sportId, Name = "Badminton" },
            Code = "B1",
            Name = "Court B1",
            BasePricePerHour = 100000,
            Status = CourtStatus.Active
        };

        var repo = new FakeCourtRepository();
        repo.Courts.Add(court);

        var service = new CourtService(repo);

        var all = await service.GetAllAsync(sportCenterId: centerId);
        Assert.Single(all);
        Assert.Equal("Court B1", all[0].Name);
        Assert.Equal("Arena", all[0].SportCenterName);
        Assert.Equal("Badminton", all[0].SportName);

        var single = await service.GetByIdAsync(court.Id);
        Assert.Equal("Court B1", single.Name);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }
}
