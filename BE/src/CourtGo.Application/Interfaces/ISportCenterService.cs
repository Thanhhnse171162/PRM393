using CourtGo.Application.SportCenters;

namespace CourtGo.Application.Interfaces;

public interface ISportCenterService
{
    Task<IReadOnlyList<SportCenterSummaryDto>> GetAllAsync(
        Guid? sportId = null,
        string? city = null,
        string? district = null,
        string? search = null,
        CancellationToken ct = default);

    Task<SportCenterDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);
}
