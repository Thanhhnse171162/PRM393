using CourtGo.Application.Bookings;

namespace CourtGo.Application.Interfaces;

public interface IPaymentReconciliationService
{
    Task<BookingPaymentStatusResponse> ReconcileAsync(Guid customerUserId, Guid bookingId,
        CancellationToken cancellationToken = default);
}
