using CourtGo.Application.AdminDashboard;
using CourtGo.Application.Common;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class AdminDashboardService(
    CourtGoDbContext db,
    TimeProvider clock
) : IAdminDashboardService
{
    public async Task<AdminDashboardResponse> GetDashboardAsync(
        AdminDashboardQuery query, CancellationToken cancellationToken = default)
    {
        string timeZoneId = "Asia/Ho_Chi_Minh";
        if (query.CenterId is Guid centerId)
        {
            var centerTz = await db.SportCenters.AsNoTracking()
                .Where(c => c.Id == centerId)
                .Select(c => c.TimeZoneId)
                .SingleOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(centerTz))
            {
                timeZoneId = centerTz;
            }
        }

        var tz = TimeZoneHelper.ResolveTimeZone(timeZoneId);
        var targetDate = query.Date ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), tz).DateTime);

        var localStart = targetDate.ToDateTime(TimeOnly.MinValue);
        var localEnd = targetDate.ToDateTime(TimeOnly.MaxValue);
        var startUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, tz));
        var endUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, tz));

        var bookings = db.Bookings.AsNoTracking().Where(b => b.StartAt < endUtc && b.EndAt > startUtc);
        if (query.CenterId is Guid cid)
        {
            bookings = bookings.Where(b => b.Court!.SportCenterId == cid);
        }

        var totalBookings = await bookings.CountAsync(cancellationToken);
        var pendingBookings = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.PendingPayment, cancellationToken);
        var confirmedBookings = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.Confirmed, cancellationToken);
        var checkedInBookings = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.CheckedIn, cancellationToken);
        var inProgressBookings = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.InProgress, cancellationToken);
        var completedBookings = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.Completed, cancellationToken);
        var cancelledBookings = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.Cancelled, cancellationToken);
        var noShowBookings = await bookings.CountAsync(b => b.BookingStatus == BookingStatus.NoShow, cancellationToken);

        var payments = db.Payments.AsNoTracking().Where(p => p.TransactionStatus == PaymentTransactionStatus.Succeeded);
        if (query.CenterId is Guid pcid)
        {
            payments = payments.Where(p => p.Booking!.Court!.SportCenterId == pcid);
        }

        var dayPayments = payments.Where(p => (p.PaidAt ?? p.CreatedAt) >= startUtc && (p.PaidAt ?? p.CreatedAt) <= endUtc);

        var grossCharges = await dayPayments
            .Where(p => p.PaymentKind == PaymentKind.Deposit || p.PaymentKind == PaymentKind.Remaining)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var refunds = await dayPayments
            .Where(p => p.PaymentKind == PaymentKind.Refund)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var todayRevenue = grossCharges - refunds;

        var activeCenters = await db.SportCenters.AsNoTracking()
            .CountAsync(c => c.Status == SportCenterStatus.Active && (query.CenterId == null || c.Id == query.CenterId), cancellationToken);

        var activeCourts = await db.Courts.AsNoTracking()
            .CountAsync(c => c.Status == CourtStatus.Active && (query.CenterId == null || c.SportCenterId == query.CenterId), cancellationToken);

        var now = clock.GetUtcNow();
        var upcomingBookings = await db.Bookings.AsNoTracking()
            .Where(b => (query.CenterId == null || b.Court!.SportCenterId == query.CenterId) &&
                        b.BookingStatus == BookingStatus.Confirmed &&
                        b.StartAt > now)
            .CountAsync(cancellationToken);

        var pendingCancellationRequests = await db.CancellationRequests.AsNoTracking()
            .Where(r => r.Status == CancellationRequestStatus.Pending &&
                        (query.CenterId == null || r.Booking!.Court!.SportCenterId == query.CenterId))
            .CountAsync(cancellationToken);

        var occupancy = await AdminOccupancyCalculator.CalculateOccupancyAsync(
            db, targetDate, targetDate, query.CenterId, cancellationToken);

        return new AdminDashboardResponse(
            targetDate,
            timeZoneId,
            query.CenterId,
            totalBookings,
            pendingBookings,
            confirmedBookings,
            checkedInBookings,
            inProgressBookings,
            completedBookings,
            cancelledBookings,
            noShowBookings,
            todayRevenue,
            refunds,
            activeCenters,
            activeCourts,
            occupancy.OverallOccupancyRate,
            upcomingBookings,
            pendingCancellationRequests
        );
    }
}
