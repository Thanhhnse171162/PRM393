using CourtGo.Application.Bookings;
using CourtGo.Application.Courts;

namespace CourtGo.Application.Interfaces;

public interface IAdminCourtService
{
    Task<PagedResult<AdminCourtListItemDto>> GetCourtsAsync(AdminCourtQuery query, CancellationToken ct = default);
    Task<AdminCourtDetailDto> GetCourtByIdAsync(Guid courtId, CancellationToken ct = default);
    Task<AdminCourtDetailDto> CreateCourtAsync(CreateCourtRequest request, CancellationToken ct = default);
    Task<AdminCourtDetailDto> UpdateCourtAsync(Guid courtId, UpdateCourtRequest request, CancellationToken ct = default);
    Task<AdminCourtDetailDto> UpdateCourtStatusAsync(Guid courtId, UpdateCourtStatusRequest request, CancellationToken ct = default);
}
