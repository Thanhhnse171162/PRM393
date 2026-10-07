using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Repositories;

public class SportCenterRepository : ISportCenterRepository
{
    private readonly CourtGoDbContext _db;

    public SportCenterRepository(CourtGoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SportCenter>> GetAllAsync(
        Guid? sportId = null,
        string? city = null,
        string? district = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var query = _db.SportCenters
            .AsNoTracking()
            .Where(c => c.Status == SportCenterStatus.Active)
            .Include(c => c.Courts.Where(ct => ct.Status == CourtStatus.Active))
                .ThenInclude(ct => ct.Sport)
            .AsQueryable();

        if (sportId.HasValue)
        {
            query = query.Where(c => c.Courts.Any(ct => ct.SportId == sportId.Value && ct.Status == CourtStatus.Active));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var trimmedCity = city.Trim();
            query = query.Where(c => c.City.ToLower() == trimmedCity.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(district))
        {
            var trimmedDistrict = district.Trim();
            query = query.Where(c => c.District.ToLower() == trimmedDistrict.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim();
            query = query.Where(c => c.Name.Contains(trimmedSearch) || c.AddressLine.Contains(trimmedSearch));
        }

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<SportCenter?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.SportCenters
            .AsNoTracking()
            .Include(c => c.OperatingHours)
            .Include(c => c.Courts.Where(ct => ct.Status == CourtStatus.Active))
                .ThenInclude(ct => ct.Sport)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}
