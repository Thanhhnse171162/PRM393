using CourtGo.Application.Bookings;

namespace CourtGo.Application.Interfaces;

public interface IBookingQueryService
{
    Task<PagedResult<BookingListItemDto>> GetMyBookingsAsync(Guid customerUserId, BookingHistoryQuery query, CancellationToken cancellationToken = default);
    Task<BookingDetailDto> GetBookingDetailAsync(Guid customerUserId, Guid bookingId, CancellationToken cancellationToken = default);
    Task<BookingQrResponse> GetQrAsync(Guid customerUserId, Guid bookingId, CancellationToken cancellationToken = default);
}
