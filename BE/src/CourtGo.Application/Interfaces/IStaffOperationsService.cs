using CourtGo.Application.Bookings;
using CourtGo.Application.Operations;
namespace CourtGo.Application.Interfaces;
public interface IStaffOperationsService
{
    Task<StaffDashboardDto> GetDashboardAsync(Guid staffId, CancellationToken cancellationToken = default);
    Task<PagedResult<StaffScheduleItem>> GetBookingsAsync(Guid staffId, StaffScheduleQuery query, CancellationToken cancellationToken = default);
    Task<StaffBookingDetail> GetBookingAsync(Guid staffId, Guid bookingId, CancellationToken cancellationToken = default);
}
