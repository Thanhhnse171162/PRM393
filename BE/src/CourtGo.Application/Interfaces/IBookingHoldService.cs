using CourtGo.Application.Bookings;

namespace CourtGo.Application.Interfaces;

public interface IBookingHoldService
{
    Task<BookingHoldResponse> HoldAsync(
        Guid customerUserId,
        BookingHoldRequest request,
        CancellationToken cancellationToken = default
    );
}
