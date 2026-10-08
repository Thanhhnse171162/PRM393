using CourtGo.Application.Bookings;
using CourtGo.Application.SportCenters;

namespace CourtGo.Application.Interfaces;

public interface IAdminSportCenterService
{
    Task<PagedResult<AdminSportCenterListItemDto>> GetSportCentersAsync(AdminSportCenterQuery query, CancellationToken ct = default);
    Task<AdminSportCenterDetailDto> GetSportCenterByIdAsync(Guid centerId, CancellationToken ct = default);
    Task<AdminSportCenterDetailDto> CreateSportCenterAsync(CreateSportCenterRequest request, CancellationToken ct = default);
    Task<AdminSportCenterDetailDto> UpdateSportCenterAsync(Guid centerId, UpdateSportCenterRequest request, CancellationToken ct = default);
    Task<AdminSportCenterDetailDto> UpdateSportCenterStatusAsync(Guid centerId, UpdateSportCenterStatusRequest request, CancellationToken ct = default);
}
