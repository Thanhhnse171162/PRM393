namespace CourtGo.Application.Interfaces;
public record BookingLifecycleResult(Guid BookingId, string BookingStatus);
public interface IBookingLifecycleService
{
    Task<int> AdvanceAsync(CancellationToken cancellationToken = default);
    Task<BookingLifecycleResult> MarkNoShowAsync(Guid staffId, Guid bookingId, CancellationToken cancellationToken = default);
}
