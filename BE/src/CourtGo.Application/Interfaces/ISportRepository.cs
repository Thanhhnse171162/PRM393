using CourtGo.Domain.Entities;

namespace CourtGo.Application.Interfaces;

public interface ISportRepository
{
    Task<IReadOnlyList<Sport>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<Sport?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
