using CourtGo.Application.AdminBookings;
using CourtGo.Application.Bookings;

namespace CourtGo.Application.Interfaces;

public interface IAdminBookingService
{
    Task<PagedResult<AdminBookingListItemResponse>> GetBookingsAsync(AdminBookingQuery query, CancellationToken cancellationToken = default);
    Task<AdminBookingDetailResponse> GetBookingDetailAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<AdminCancelBookingResponse> CancelBookingAsync(Guid adminUserId, Guid bookingId, AdminCancelBookingRequest request, CancellationToken cancellationToken = default);
}
