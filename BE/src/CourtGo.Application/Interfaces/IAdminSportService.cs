using CourtGo.Application.Bookings;
using CourtGo.Application.Sports;

namespace CourtGo.Application.Interfaces;

public interface IAdminSportService
{
    Task<PagedResult<AdminSportDto>> GetSportsAsync(AdminSportQuery query, CancellationToken ct = default);
    Task<AdminSportDetailDto> GetSportByIdAsync(Guid sportId, CancellationToken ct = default);
    Task<AdminSportDetailDto> CreateSportAsync(CreateSportRequest request, CancellationToken ct = default);
    Task<AdminSportDetailDto> UpdateSportAsync(Guid sportId, UpdateSportRequest request, CancellationToken ct = default);
    Task<AdminSportDetailDto> UpdateSportStatusAsync(Guid sportId, UpdateSportStatusRequest request, CancellationToken ct = default);
}
