using CourtGo.Application.Bookings;
using CourtGo.Application.Staff;

namespace CourtGo.Application.Interfaces;

public interface IAdminStaffService
{
    Task<PagedResult<AdminStaffSummaryDto>> GetStaffListAsync(AdminStaffQuery query, CancellationToken ct = default);
    Task<AdminStaffDetailDto> GetStaffDetailAsync(Guid staffId, CancellationToken ct = default);
    Task<AdminStaffDetailDto> CreateStaffAsync(CreateStaffRequest request, CancellationToken ct = default);
    Task<AdminStaffDetailDto> UpdateStaffProfileAsync(Guid staffId, UpdateStaffProfileRequest request, CancellationToken ct = default);
    Task<AdminStaffDetailDto> UpdateStaffStatusAsync(Guid staffId, UpdateStaffStatusRequest request, CancellationToken ct = default);
    Task<AdminStaffDetailDto> ReassignStaffAsync(Guid staffId, ReassignStaffRequest request, CancellationToken ct = default);
}
