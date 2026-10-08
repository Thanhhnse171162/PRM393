using System.Linq.Expressions;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Operations;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Domain.Rules;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace CourtGo.Infrastructure.Services;

public class StaffOperationsService(CourtGoDbContext db, TimeProvider clock) : IStaffOperationsService
{
    internal static readonly Expression<Func<Booking, StaffScheduleItem>> ScheduleProjection = b => new(
        b.Id, b.BookingCode, b.CustomerNameSnapshot, b.CustomerPhoneSnapshot, b.CourtId, b.CourtNameSnapshot,
        b.SportNameSnapshot, b.StartAt, b.EndAt, b.BookingStatus.ToString(), b.PaymentStatus.ToString(), b.TotalAmount);

    public async Task<PagedResult<StaffScheduleItem>> GetBookingsAsync(Guid staffId, StaffScheduleQuery query, CancellationToken cancellationToken = default)
    {
        var center = await StaffAccess.GetCenterAsync(db, staffId, cancellationToken);
        ValidatePage(query.PageNumber, query.PageSize);
        var bookings = db.Bookings.AsNoTracking().Where(b => b.Court!.SportCenterId == center);
        if (query.Date is DateOnly date)
        {
            var (start, end) = await DayRangeAsync(center, date, cancellationToken);
            bookings = bookings.Where(b => b.StartAt < end && b.EndAt > start);
        }
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<BookingStatus>(query.Status, true, out var status) || !Enum.IsDefined(status))
                throw new ValidationException("Invalid booking status.");
            bookings = bookings.Where(b => b.BookingStatus == status);
        }
        if (query.SportId is Guid sport) bookings = bookings.Where(b => b.Court!.SportId == sport);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (search.Length > 200) throw new ValidationException("Search exceeds 200 characters.");
            bookings = bookings.Where(b => b.BookingCode.Contains(search) || b.CustomerNameSnapshot.Contains(search) || b.CustomerPhoneSnapshot.Contains(search));
        }
        var count = await bookings.CountAsync(cancellationToken);
        var items = await bookings.OrderBy(b => b.StartAt).ThenBy(b => b.Id)
            .Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(ScheduleProjection).ToListAsync(cancellationToken);
        return new(items, query.PageNumber, query.PageSize, count, (int)Math.Ceiling(count / (double)query.PageSize));
    }

    public async Task<StaffBookingDetail> GetBookingAsync(Guid staffId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var center = await StaffAccess.GetCenterAsync(db, staffId, cancellationToken);
        var item = await db.Bookings.AsNoTracking().Where(b => b.Id == bookingId && b.Court!.SportCenterId == center)
            .Select(ScheduleProjection).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Booking not found.", ErrorCodes.BookingNotFound);
        var deposit = await db.Bookings.Where(b => b.Id == bookingId).Select(b => b.DepositAmount).SingleAsync(cancellationToken);
        var paid = await BookingPaymentQueries.GetPaidAmountAsync(db, bookingId, cancellationToken);
        var slots = await db.BookingSlots.AsNoTracking().Where(s => s.BookingId == bookingId).OrderBy(s => s.StartAt)
            .Select(s => new BookingSlotDto(s.StartAt, s.EndAt, s.UnitPrice)).ToListAsync(cancellationToken);
        var payments = await db.Payments.AsNoTracking().Where(p => p.BookingId == bookingId).OrderBy(p => p.CreatedAt).ThenBy(p => p.Id)
            .Select(p => new PaymentEntryDto(p.Id, p.PaymentKind.ToString(), p.PaymentMethod.ToString(), p.Amount, p.TransactionStatus.ToString(), p.PaidAt))
            .ToListAsync(cancellationToken);
        var checkIn = await db.CheckIns.AsNoTracking().Where(c => c.BookingId == bookingId)
            .Select(c => new CheckInEntryDto(c.StaffUserId, c.CheckedInAt, c.OutstandingPaymentOverride, c.OverrideReason)).SingleOrDefaultAsync(cancellationToken);
        return new(item, new(item.TotalAmount, deposit, paid, PaymentRules.CalculateRemaining(item.TotalAmount, paid)), slots, payments, checkIn);
    }

    public async Task<StaffDashboardDto> GetDashboardAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var center = await StaffAccess.GetCenterAsync(db, staffId, cancellationToken);
        var zone = await db.SportCenters.Where(c => c.Id == center).Select(c => c.TimeZoneId).SingleAsync(cancellationToken);
        var now = clock.GetUtcNow();
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, TimeZoneHelper.ResolveTimeZone(zone)).DateTime);
        var (start, end) = await DayRangeAsync(center, date, cancellationToken);
        var all = db.Bookings.AsNoTracking().Where(b => b.Court!.SportCenterId == center);
        var today = all.Where(b => b.StartAt < end && b.EndAt > start);
        var waiting = await today.CountAsync(b => b.BookingStatus == BookingStatus.Confirmed, cancellationToken);
        var playing = await today.CountAsync(b => b.BookingStatus == BookingStatus.CheckedIn || b.BookingStatus == BookingStatus.InProgress, cancellationToken);
        var upcoming = await all.CountAsync(b => b.BookingStatus == BookingStatus.Confirmed && b.StartAt > now, cancellationToken);
        var attention = await today.CountAsync(b => (b.BookingStatus == BookingStatus.Confirmed && b.StartAt < now)
            || (b.BookingStatus == BookingStatus.CheckedIn && b.TotalAmount > b.Payments
                .Where(p => p.TransactionStatus == PaymentTransactionStatus.Succeeded && (p.PaymentKind == PaymentKind.Deposit || p.PaymentKind == PaymentKind.Remaining))
                .Sum(p => p.Amount)), cancellationToken);
        var next = await all.Where(b => b.BookingStatus == BookingStatus.Confirmed && b.EndAt > now)
            .OrderBy(b => b.StartAt).ThenBy(b => b.Id).Take(10).Select(ScheduleProjection).ToListAsync(cancellationToken);
        return new(center, date, waiting, playing, upcoming, attention, next, await CourtStatesAsync(center, cancellationToken));
    }

    internal async Task<List<CourtStateDto>> CourtStatesAsync(Guid center, CancellationToken ct, int page = 1, int size = 100, Guid? courtId = null)
    {
        var now = clock.GetUtcNow();
        return await db.Courts.AsNoTracking().Where(c => c.SportCenterId == center && (courtId == null || c.Id == courtId))
            .OrderBy(c => c.Code).ThenBy(c => c.Id).Skip((page - 1) * size).Take(size)
            .Select(c => new CourtStateDto(c.Id, c.Name, c.Sport!.Name, c.Status.ToString(),
                c.Status == CourtStatus.Inactive ? "Inactive" :
                c.Status == CourtStatus.Maintenance ? "Maintenance" :
                db.CourtBlocks.Any(b => b.CourtId == c.Id && b.Type == CourtBlockType.Maintenance && b.StartAt <= now && b.EndAt > now) ? "Maintenance" :
                db.CourtBlocks.Any(b => b.CourtId == c.Id && b.StartAt <= now && b.EndAt > now) ? "Blocked" :
                db.Bookings.Any(b => b.CourtId == c.Id && b.StartAt <= now && b.EndAt > now && (b.BookingStatus == BookingStatus.CheckedIn || b.BookingStatus == BookingStatus.InProgress)) ? "Playing" :
                db.Bookings.Any(b => b.CourtId == c.Id && b.StartAt <= now && b.EndAt > now && b.BookingStatus == BookingStatus.Confirmed) ? "WaitingCustomer" : "Available"))
            .ToListAsync(ct);
    }

    private async Task<(DateTimeOffset Start, DateTimeOffset End)> DayRangeAsync(Guid center, DateOnly date, CancellationToken ct)
    {
        var zone = TimeZoneHelper.ResolveTimeZone(await db.SportCenters.Where(c => c.Id == center).Select(c => c.TimeZoneId).SingleAsync(ct));
        return (new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), zone)),
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(date.AddDays(1).ToDateTime(TimeOnly.MinValue), zone)));
    }

    internal static void ValidatePage(int page, int size)
    {
        if (page < 1 || size is < 1 or > 100 || page > int.MaxValue / size)
            throw new ValidationException("pageNumber must be positive; pageSize must be between 1 and 100.");
    }
}
