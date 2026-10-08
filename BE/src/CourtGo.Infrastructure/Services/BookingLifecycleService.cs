using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace CourtGo.Infrastructure.Services;
public class BookingLifecycleService(CourtGoDbContext db, BookingCommandExecutor commands, TimeProvider clock) : IBookingLifecycleService
{
    public async Task<int> AdvanceAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        var ids = await db.Bookings.AsNoTracking().Where(b =>
            (b.BookingStatus == BookingStatus.CheckedIn && b.StartAt <= now) ||
            (b.BookingStatus == BookingStatus.InProgress && b.EndAt <= now))
            .OrderBy(b => b.EndAt).ThenBy(b => b.Id).Select(b => b.Id).Take(200).ToListAsync(cancellationToken);
        var count = 0;
        foreach (var id in ids)
            count += await commands.ExecuteAsync(id, (booking, ct) =>
            {
                var current = clock.GetUtcNow();
                var next = booking.BookingStatus switch
                {
                    BookingStatus.CheckedIn when booking.EndAt <= current => BookingStatus.Completed,
                    BookingStatus.CheckedIn when booking.StartAt <= current => BookingStatus.InProgress,
                    BookingStatus.InProgress when booking.EndAt <= current => BookingStatus.Completed,
                    _ => booking.BookingStatus
                };
                if (next == booking.BookingStatus) return Task.FromResult(0);
                Transition(booking, next, null, "Scheduled booking lifecycle", current);
                return Task.FromResult(1);
            }, cancellationToken);
        return count;
    }
    public Task<BookingLifecycleResult> MarkNoShowAsync(Guid staffId, Guid bookingId, CancellationToken cancellationToken = default)
        => commands.ExecuteAsync(bookingId, async (booking, ct) =>
        {
            var center = await StaffAccess.GetCenterAsync(db, staffId, ct);
            if (!await db.Courts.AnyAsync(c => c.Id == booking.CourtId && c.SportCenterId == center, ct))
                throw new ForbiddenException("Booking belongs to another center.", ErrorCodes.StaffCenterAccessDenied);
            var now = clock.GetUtcNow();
            if (booking.BookingStatus != BookingStatus.Confirmed || now < booking.EndAt)
                throw new ConflictException("Only confirmed bookings whose end time has passed can be marked no-show.");
            Transition(booking, BookingStatus.NoShow, staffId, "Staff marked customer no-show", now);
            return new BookingLifecycleResult(booking.Id, booking.BookingStatus.ToString());
        }, cancellationToken);
    private void Transition(Booking booking, BookingStatus next, Guid? actor, string reason, DateTimeOffset now)
    {
        db.BookingStatusHistories.Add(new BookingStatusHistory { BookingId = booking.Id, FromStatus = booking.BookingStatus,
            ToStatus = next, ChangedByUserId = actor, Reason = reason, CreatedAt = now });
        booking.BookingStatus = next;
    }
}
