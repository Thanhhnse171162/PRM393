namespace CourtGo.Application.Interfaces;

public interface IExpiredBookingHoldService
{
    Task ReleaseExpiredHoldsAsync(CancellationToken ct = default);
}
