using CourtGo.Application.AdminReports;
using CourtGo.Application.Common;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class AdminReportService(
    CourtGoDbContext db
) : IAdminReportService
{
    public async Task<AdminReportSummaryResponse> GetSummaryReportAsync(
        AdminReportSummaryQuery query, CancellationToken cancellationToken = default)
    {
        ValidateDateRange(query.DateFrom, query.DateTo);
        var (startUtc, endUtc) = GetUtcRange(query.DateFrom, query.DateTo);

        var bookings = db.Bookings.AsNoTracking().Where(b => b.StartAt >= startUtc && b.StartAt <= endUtc);
        if (query.CenterId is Guid centerId)
        {
            bookings = bookings.Where(b => b.Court!.SportCenterId == centerId);
        }

        var totalBookings = await bookings.CountAsync(cancellationToken);
        var completedBookings = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.Completed, cancellationToken);
        var cancelledBookings = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.Cancelled, cancellationToken);
        var noShowBookings = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.NoShow, cancellationToken);

        var payments = db.Payments.AsNoTracking().Where(p => p.TransactionStatus == PaymentTransactionStatus.Succeeded);
        if (query.CenterId is Guid cid)
        {
            payments = payments.Where(p => p.Booking!.Court!.SportCenterId == cid);
        }

        var rangePayments = payments.Where(p => (p.PaidAt ?? p.CreatedAt) >= startUtc && (p.PaidAt ?? p.CreatedAt) <= endUtc);

        var grossCollected = await rangePayments
            .Where(p => p.PaymentKind == PaymentKind.Deposit || p.PaymentKind == PaymentKind.Remaining)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var refundAmount = await rangePayments
            .Where(p => p.PaymentKind == PaymentKind.Refund)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var netRevenue = grossCollected - refundAmount;
        var averageBookingValue = totalBookings > 0
            ? Math.Round(netRevenue / totalBookings, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var occupancy = await AdminOccupancyCalculator.CalculateOccupancyAsync(
            db, query.DateFrom, query.DateTo, query.CenterId, cancellationToken);

        return new AdminReportSummaryResponse(
            query.DateFrom,
            query.DateTo,
            query.CenterId,
            totalBookings,
            completedBookings,
            cancelledBookings,
            noShowBookings,
            grossCollected,
            refundAmount,
            netRevenue,
            averageBookingValue,
            occupancy.OverallOccupancyRate
        );
    }

    public async Task<AdminRevenueReportResponse> GetRevenueReportAsync(
        AdminRevenueReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateDateRange(query.DateFrom, query.DateTo);
        var (startUtc, endUtc) = GetUtcRange(query.DateFrom, query.DateTo);

        var normalizedGroup = (query.GroupBy ?? "day").Trim().ToLowerInvariant();
        if (normalizedGroup is not ("day" or "month" or "center" or "sport" or "paymentmethod"))
        {
            throw new ValidationException("groupBy must be 'day', 'month', 'center', 'sport', or 'paymentMethod'.");
        }

        var payments = db.Payments.AsNoTracking()
            .Include(p => p.Booking)
            .Where(p => p.TransactionStatus == PaymentTransactionStatus.Succeeded);

        if (query.CenterId is Guid cid)
        {
            payments = payments.Where(p => p.Booking!.Court!.SportCenterId == cid);
        }

        var rangePayments = await payments
            .Where(p => (p.PaidAt ?? p.CreatedAt) >= startUtc && (p.PaidAt ?? p.CreatedAt) <= endUtc)
            .Select(p => new
            {
                p.Id,
                p.PaymentKind,
                p.PaymentMethod,
                p.Amount,
                Timestamp = p.PaidAt ?? p.CreatedAt,
                CenterName = p.Booking != null ? p.Booking.CenterNameSnapshot : string.Empty,
                SportName = p.Booking != null ? p.Booking.SportNameSnapshot : string.Empty
            })
            .ToListAsync(cancellationToken);

        var items = new List<AdminRevenueSeriesItemDto>();

        switch (normalizedGroup)
        {
            case "month":
                var monthGroups = rangePayments.GroupBy(p => p.Timestamp.ToString("yyyy-MM")).OrderBy(g => g.Key);
                foreach (var g in monthGroups)
                {
                    var gross = g.Where(p => p.PaymentKind is PaymentKind.Deposit or PaymentKind.Remaining).Sum(p => p.Amount);
                    var refund = g.Where(p => p.PaymentKind == PaymentKind.Refund).Sum(p => p.Amount);
                    items.Add(new AdminRevenueSeriesItemDto(g.Key, $"Tháng {g.Key}", gross, refund, gross - refund, g.Count()));
                }
                break;

            case "center":
                var centerGroups = rangePayments.GroupBy(p => p.CenterName).OrderBy(g => g.Key);
                foreach (var g in centerGroups)
                {
                    var gross = g.Where(p => p.PaymentKind is PaymentKind.Deposit or PaymentKind.Remaining).Sum(p => p.Amount);
                    var refund = g.Where(p => p.PaymentKind == PaymentKind.Refund).Sum(p => p.Amount);
                    items.Add(new AdminRevenueSeriesItemDto(g.Key, g.Key, gross, refund, gross - refund, g.Count()));
                }
                break;

            case "sport":
                var sportGroups = rangePayments.GroupBy(p => p.SportName).OrderBy(g => g.Key);
                foreach (var g in sportGroups)
                {
                    var gross = g.Where(p => p.PaymentKind is PaymentKind.Deposit or PaymentKind.Remaining).Sum(p => p.Amount);
                    var refund = g.Where(p => p.PaymentKind == PaymentKind.Refund).Sum(p => p.Amount);
                    items.Add(new AdminRevenueSeriesItemDto(g.Key, g.Key, gross, refund, gross - refund, g.Count()));
                }
                break;

            case "paymentmethod":
                var methodGroups = rangePayments.GroupBy(p => p.PaymentMethod.ToString()).OrderBy(g => g.Key);
                foreach (var g in methodGroups)
                {
                    var gross = g.Where(p => p.PaymentKind is PaymentKind.Deposit or PaymentKind.Remaining).Sum(p => p.Amount);
                    var refund = g.Where(p => p.PaymentKind == PaymentKind.Refund).Sum(p => p.Amount);
                    items.Add(new AdminRevenueSeriesItemDto(g.Key, g.Key, gross, refund, gross - refund, g.Count()));
                }
                break;

            case "day":
            default:
                for (var date = query.DateFrom; date <= query.DateTo; date = date.AddDays(1))
                {
                    var dateStr = date.ToString("yyyy-MM-dd");
                    var dayItems = rangePayments.Where(p => p.Timestamp.ToString("yyyy-MM-dd") == dateStr).ToList();
                    var gross = dayItems.Where(p => p.PaymentKind is PaymentKind.Deposit or PaymentKind.Remaining).Sum(p => p.Amount);
                    var refund = dayItems.Where(p => p.PaymentKind == PaymentKind.Refund).Sum(p => p.Amount);
                    items.Add(new AdminRevenueSeriesItemDto(dateStr, dateStr, gross, refund, gross - refund, dayItems.Count));
                }
                break;
        }

        var totalGross = items.Sum(i => i.GrossCollected);
        var totalRefund = items.Sum(i => i.RefundAmount);

        return new AdminRevenueReportResponse(
            query.DateFrom,
            query.DateTo,
            query.CenterId,
            normalizedGroup,
            totalGross,
            totalRefund,
            totalGross - totalRefund,
            items
        );
    }

    public async Task<AdminBookingReportResponse> GetBookingReportAsync(
        AdminBookingReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateDateRange(query.DateFrom, query.DateTo);
        var (startUtc, endUtc) = GetUtcRange(query.DateFrom, query.DateTo);

        var bookings = db.Bookings.AsNoTracking().Where(b => b.StartAt >= startUtc && b.StartAt <= endUtc);
        if (query.CenterId is Guid centerId)
        {
            bookings = bookings.Where(b => b.Court!.SportCenterId == centerId);
        }

        var bookingList = await bookings.Select(b => new
        {
            b.Id,
            b.BookingStatus,
            b.CenterNameSnapshot,
            b.SportNameSnapshot,
            b.StartAt
        }).ToListAsync(cancellationToken);

        var totalBookings = bookingList.Count;
        var cancelledCount = bookingList.Count(b => b.BookingStatus == BookingStatus.Cancelled);
        var noShowCount = bookingList.Count(b => b.BookingStatus == BookingStatus.NoShow);

        var byStatus = bookingList.GroupBy(b => b.BookingStatus.ToString())
            .Select(g => new AdminCategoryCountDto(g.Key, g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var bySport = bookingList.GroupBy(b => b.SportNameSnapshot)
            .Select(g => new AdminCategoryCountDto(g.Key, g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var byCenter = bookingList.GroupBy(b => b.CenterNameSnapshot)
            .Select(g => new AdminCategoryCountDto(g.Key, g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var trends = new List<AdminBookingTrendDto>();
        for (var date = query.DateFrom; date <= query.DateTo; date = date.AddDays(1))
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var dayBookings = bookingList.Where(b => b.StartAt.ToString("yyyy-MM-dd") == dateStr).ToList();
            trends.Add(new AdminBookingTrendDto(
                date,
                dayBookings.Count,
                dayBookings.Count(b => b.BookingStatus == BookingStatus.Cancelled),
                dayBookings.Count(b => b.BookingStatus == BookingStatus.NoShow)
            ));
        }

        return new AdminBookingReportResponse(
            query.DateFrom,
            query.DateTo,
            query.CenterId,
            totalBookings,
            cancelledCount,
            noShowCount,
            byStatus,
            bySport,
            byCenter,
            trends
        );
    }

    public async Task<AdminOccupancyReportResponse> GetOccupancyReportAsync(
        AdminOccupancyReportQuery query, CancellationToken cancellationToken = default)
    {
        ValidateDateRange(query.DateFrom, query.DateTo);
        return await AdminOccupancyCalculator.CalculateOccupancyAsync(
            db, query.DateFrom, query.DateTo, query.CenterId, cancellationToken);
    }

    private static void ValidateDateRange(DateOnly dateFrom, DateOnly dateTo)
    {
        if (dateFrom > dateTo)
        {
            throw new ValidationException("dateFrom must be less than or equal to dateTo.");
        }
        if (dateTo.DayNumber - dateFrom.DayNumber > 366)
        {
            throw new ValidationException("Date range cannot exceed 366 days.");
        }
    }

    private static (DateTimeOffset startUtc, DateTimeOffset endUtc) GetUtcRange(DateOnly from, DateOnly to)
    {
        var tz = TimeZoneHelper.ResolveTimeZone("Asia/Ho_Chi_Minh");
        var localStart = from.ToDateTime(TimeOnly.MinValue);
        var localEnd = to.ToDateTime(TimeOnly.MaxValue);
        return (
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, tz)),
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, tz))
        );
    }
}
