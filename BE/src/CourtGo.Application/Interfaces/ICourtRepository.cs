using CourtGo.Domain.Entities;

namespace CourtGo.Application.Interfaces;

public interface ICourtRepository
{
    Task<IReadOnlyList<Court>> GetAllAsync(
        Guid? sportCenterId = null,
        Guid? sportId = null,
        bool activeOnly = true,
        CancellationToken ct = default);

    Task<Court?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
