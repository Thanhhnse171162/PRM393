using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Domain.Rules;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class BookingQueryService(CourtGoDbContext dbContext, TimeProvider timeProvider) : IBookingQueryService
{
    private readonly CourtGoDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<PagedResult<BookingListItemDto>> GetMyBookingsAsync(
        Guid customerUserId, BookingHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var group = (query.StatusGroup ?? "upcoming").ToLowerInvariant();
        if (group is not ("upcoming" or "completed" or "cancelled"))
            throw new ValidationException("statusGroup must be upcoming, completed or cancelled.", ErrorCodes.InvalidBookingStatusGroup);
        if (query.PageNumber < 1 || query.PageSize is < 1 or > 100 ||
            (long)(query.PageNumber - 1) * query.PageSize > int.MaxValue)
            throw new ValidationException("pageNumber must be positive and pageSize must be between 1 and 100.");

        var now = _timeProvider.GetUtcNow();
        var bookings = _dbContext.Bookings.AsNoTracking().Where(b => b.CustomerUserId == customerUserId);
        bookings = group switch
        {
            "upcoming" => bookings.Where(b =>
                (b.BookingStatus == BookingStatus.PendingPayment && b.HoldExpiresAt != null && b.HoldExpiresAt > now) ||
                b.BookingStatus == BookingStatus.Confirmed || b.BookingStatus == BookingStatus.CheckedIn ||
                b.BookingStatus == BookingStatus.InProgress),
            "completed" => bookings.Where(b => b.BookingStatus == BookingStatus.Completed),
            _ => bookings.Where(b => b.BookingStatus == BookingStatus.Cancelled ||
                b.BookingStatus == BookingStatus.Expired || b.BookingStatus == BookingStatus.NoShow)
        };
        var totalItems = await bookings.CountAsync(cancellationToken);
        var sorted = group == "upcoming"
            ? bookings.OrderBy(b => b.StartAt).ThenBy(b => b.Id)
            : bookings.OrderByDescending(b => b.StartAt).ThenBy(b => b.Id);
        // Both paging and the correlated payment SUM execute in SQL.
        var rows = await sorted.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize)
            .Select(b => new
            {
                b.Id, b.BookingCode, b.BookingStatus, b.PaymentStatus,
                b.CourtNameSnapshot, b.CenterNameSnapshot, b.SportNameSnapshot,
                b.StartAt, b.EndAt, b.DurationMinutes, b.TotalAmount, b.DepositAmount,
                PaidAmount = _dbContext.Payments.Where(PaymentRules.SuccessfulCharge).Where(p => p.BookingId == b.Id)
                    .Sum(p => (decimal?)p.Amount) ?? 0m,
                HasQr = b.BookingStatus == BookingStatus.Confirmed && b.QrToken != null && b.QrToken != "",
                b.HoldExpiresAt
            }).ToListAsync(cancellationToken);
        var items = rows.Select(b => new BookingListItemDto(
            b.Id, b.BookingCode, b.BookingStatus.ToString(), b.PaymentStatus.ToString(),
            b.CourtNameSnapshot, b.CenterNameSnapshot, b.SportNameSnapshot,
            b.StartAt, b.EndAt, b.DurationMinutes, b.TotalAmount, b.DepositAmount, b.PaidAmount,
            PaymentRules.CalculateRemaining(b.TotalAmount, b.PaidAmount), b.HasQr, b.HoldExpiresAt)).ToList();
        return new(items, query.PageNumber, query.PageSize, totalItems,
            (int)Math.Ceiling(totalItems / (double)query.PageSize));
    }

    public async Task<BookingDetailDto> GetBookingDetailAsync(
        Guid customerUserId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.Bookings.AsNoTracking()
            .Where(b => b.Id == bookingId && b.CustomerUserId == customerUserId)
            .Select(b => new
            {
                b.Id, b.BookingCode, b.BookingStatus, b.PaymentStatus,
                b.CustomerNameSnapshot, b.CustomerPhoneSnapshot,
                b.CourtId, b.CourtNameSnapshot, b.CenterNameSnapshot, b.SportNameSnapshot,
                b.StartAt, b.EndAt, b.DurationMinutes, b.TotalAmount, b.DepositAmount, b.CreatedAt,
                PaidAmount = _dbContext.Payments.Where(PaymentRules.SuccessfulCharge).Where(p => p.BookingId == b.Id)
                    .Sum(p => (decimal?)p.Amount) ?? 0m,
                HasQr = b.BookingStatus == BookingStatus.Confirmed && b.QrToken != null && b.QrToken != "",
                Slots = b.Slots.OrderBy(s => s.StartAt)
                    .Select(s => new BookingSlotDto(s.StartAt, s.EndAt, s.UnitPrice)).ToList()
            }).SingleOrDefaultAsync(cancellationToken)
            ?? throw BookingNotFound();
        return new(row.Id, row.BookingCode, row.BookingStatus.ToString(), row.PaymentStatus.ToString(),
            new(row.CustomerNameSnapshot, row.CustomerPhoneSnapshot), new(row.CenterNameSnapshot),
            new(row.CourtId, row.CourtNameSnapshot), new(row.SportNameSnapshot),
            row.StartAt, row.EndAt, row.DurationMinutes, row.Slots,
            new(row.TotalAmount, row.DepositAmount, row.PaidAmount,
                PaymentRules.CalculateRemaining(row.TotalAmount, row.PaidAmount)), row.HasQr, row.CreatedAt);
    }

    public async Task<BookingQrResponse> GetQrAsync(
        Guid customerUserId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.Bookings.AsNoTracking()
            .Where(b => b.Id == bookingId && b.CustomerUserId == customerUserId)
            .Select(b => new { b.Id, b.BookingCode, b.BookingStatus, b.QrToken,
                b.CourtNameSnapshot, b.CenterNameSnapshot, b.StartAt, b.EndAt })
            .SingleOrDefaultAsync(cancellationToken) ?? throw BookingNotFound();
        if (row.BookingStatus != BookingStatus.Confirmed || string.IsNullOrWhiteSpace(row.QrToken))
            throw new ConflictException("Check-in QR is unavailable for this booking.", ErrorCodes.QrNotAvailable);
        return new(row.Id, row.BookingCode, row.QrToken, row.CourtNameSnapshot,
            row.CenterNameSnapshot, row.StartAt, row.EndAt);
    }

    private static NotFoundException BookingNotFound() => new("Booking was not found.", ErrorCodes.BookingNotFound);
}
