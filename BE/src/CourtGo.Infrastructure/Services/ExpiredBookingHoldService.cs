using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class ExpiredBookingHoldService(CourtGoDbContext dbContext, BookingCommandExecutor commands, TimeProvider timeProvider)
    : IExpiredBookingHoldService
{
    private readonly CourtGoDbContext _dbContext = dbContext;
    private readonly BookingCommandExecutor _commands = commands;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task ReleaseExpiredHoldsAsync(CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        var candidates = await _dbContext.Bookings.AsNoTracking()
            .Where(b => b.BookingStatus == BookingStatus.PendingPayment &&
                ((b.HoldExpiresAt != null && b.HoldExpiresAt <= now) ||
                 b.Slots.Any(s => s.ReservationState == ReservationState.Held && s.HoldExpiresAt != null && s.HoldExpiresAt <= now)))
            .Select(b => b.Id).ToListAsync(ct);
        foreach (var id in candidates)
        {
            // Use the SAME booking lock and booking->slots order as payment confirmation.
            // Never act on the candidate snapshot after waiting for another transaction.
            await _commands.ExecuteAsync(id, async (booking, token) =>
            {
                if (booking.BookingStatus != BookingStatus.PendingPayment) return false;
                var slots = await _commands.ReloadSlotsAsync(booking.Id, token);
                var current = _timeProvider.GetUtcNow();
                if (!(booking.HoldExpiresAt != null && booking.HoldExpiresAt <= current) &&
                    !slots.Any(s => s.ReservationState == ReservationState.Held && s.HoldExpiresAt != null && s.HoldExpiresAt <= current))
                    return false;
                booking.BookingStatus = BookingStatus.Expired;
                booking.HoldExpiresAt = null;
                _dbContext.BookingStatusHistories.Add(new BookingStatusHistory
                {
                    BookingId = booking.Id, FromStatus = BookingStatus.PendingPayment,
                    ToStatus = BookingStatus.Expired, Reason = "Temporary booking hold expired",
                    CreatedAt = current
                });
                foreach (var slot in slots.Where(s => s.ReservationState == ReservationState.Held))
                {
                    slot.ReservationState = ReservationState.Released;
                    slot.IsOccupying = false;
                    slot.HoldExpiresAt = null;
                }
                return true;
            }, ct);
        }
    }
}
