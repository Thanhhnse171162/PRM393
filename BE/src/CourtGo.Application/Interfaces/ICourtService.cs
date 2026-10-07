using CourtGo.Application.Courts;

namespace CourtGo.Application.Interfaces;

public interface ICourtService
{
    Task<IReadOnlyList<CourtDto>> GetAllAsync(
        Guid? sportCenterId = null,
        Guid? sportId = null,
        CancellationToken ct = default);

    Task<CourtDto> GetByIdAsync(Guid id, CancellationToken ct = default);
}
