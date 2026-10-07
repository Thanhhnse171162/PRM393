using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Repositories;

public class CourtRepository : ICourtRepository
{
    private readonly CourtGoDbContext _db;

    public CourtRepository(CourtGoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Court>> GetAllAsync(
        Guid? sportCenterId = null,
        Guid? sportId = null,
        bool activeOnly = true,
        CancellationToken ct = default)
    {
        var query = _db.Courts
            .AsNoTracking()
            .Include(c => c.SportCenter)
            .Include(c => c.Sport)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(c => c.Status == CourtStatus.Active);
        }

        if (sportCenterId.HasValue)
        {
            query = query.Where(c => c.SportCenterId == sportCenterId.Value);
        }

        if (sportId.HasValue)
        {
            query = query.Where(c => c.SportId == sportId.Value);
        }

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<Court?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Courts
            .AsNoTracking()
            .Include(c => c.SportCenter)
            .Include(c => c.Sport)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}
