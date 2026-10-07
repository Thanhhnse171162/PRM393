using CourtGo.Domain.Entities;

namespace CourtGo.Application.Interfaces;

public interface ISportCenterRepository
{
    Task<IReadOnlyList<SportCenter>> GetAllAsync(
        Guid? sportId = null,
        string? city = null,
        string? district = null,
        string? search = null,
        CancellationToken ct = default);

    Task<SportCenter?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
}
