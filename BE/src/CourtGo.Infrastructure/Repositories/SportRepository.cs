using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Repositories;

public class SportRepository : ISportRepository
{
    private readonly CourtGoDbContext _db;

    public SportRepository(CourtGoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Sport>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _db.Sports.AsNoTracking().AsQueryable();

        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);
    }

    public async Task<Sport?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Sports
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }
}
