using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CourtGo.Infrastructure.Services;

public class ExpiredBookingHoldService : IExpiredBookingHoldService
{
    private readonly CourtGoDbContext _db;
    private readonly ILogger<ExpiredBookingHoldService> _logger;

    public ExpiredBookingHoldService(CourtGoDbContext db, ILogger<ExpiredBookingHoldService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ReleaseExpiredHoldsAsync(CancellationToken ct = default)
    {
        try
        {
            if (_db.Database.IsRelational())
            {
                await _db.Database.ExecuteSqlRawAsync("EXEC dbo.usp_ReleaseExpiredBookingHolds", ct);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to execute usp_ReleaseExpiredBookingHolds via SQL Server, falling back to EF Core cleanup.");
        }

        // Fallback / In-Memory provider cleanup
        var nowUtc = DateTimeOffset.UtcNow;
        var expiredBookings = await _db.Bookings
            .Where(b => b.BookingStatus == BookingStatus.PendingPayment && b.HoldExpiresAt != null && b.HoldExpiresAt <= nowUtc)
            .ToListAsync(ct);

        foreach (var b in expiredBookings)
        {
            b.BookingStatus = BookingStatus.Expired;
            b.HoldExpiresAt = null;
            b.UpdatedAt = nowUtc;
        }

        var expiredSlots = await _db.BookingSlots
            .Where(s => s.ReservationState == ReservationState.Held && s.IsOccupying && s.HoldExpiresAt != null && s.HoldExpiresAt <= nowUtc)
            .ToListAsync(ct);

        foreach (var s in expiredSlots)
        {
            s.ReservationState = ReservationState.Released;
            s.IsOccupying = false;
            s.HoldExpiresAt = null;
        }

        if (expiredBookings.Count > 0 || expiredSlots.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }
    }
}
