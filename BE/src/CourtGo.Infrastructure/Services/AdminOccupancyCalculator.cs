using CourtGo.Application.AdminReports;
using CourtGo.Application.Common;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

internal static class AdminOccupancyCalculator
{
    public static async Task<AdminOccupancyReportResponse> CalculateOccupancyAsync(
        CourtGoDbContext db,
        DateOnly dateFrom,
        DateOnly dateTo,
        Guid? centerId,
        CancellationToken ct)
    {
        var courtQuery = db.Courts.AsNoTracking()
            .Include(c => c.Sport)
            .Include(c => c.SportCenter)
            .Where(c => c.Status == CourtStatus.Active && c.SportCenter!.Status == SportCenterStatus.Active);

        if (centerId is Guid cid)
        {
            courtQuery = courtQuery.Where(c => c.SportCenterId == cid);
        }

        var courts = await courtQuery.ToListAsync(ct);
        if (courts.Count == 0)
        {
            return new AdminOccupancyReportResponse(
                dateFrom, dateTo, centerId, 0m, 0, 0, new List<AdminCourtOccupancyDto>(), new List<AdminDayOccupancyDto>());
        }

        var centerIds = courts.Select(c => c.SportCenterId).Distinct().ToList();
        var courtIds = courts.Select(c => c.Id).Distinct().ToList();

        var operatingHours = await db.OperatingHours.AsNoTracking()
            .Where(h => centerIds.Contains(h.SportCenterId))
            .ToListAsync(ct);

        var exceptions = await db.OperatingHourExceptions.AsNoTracking()
            .Where(e => centerIds.Contains(e.SportCenterId) && e.Date >= dateFrom && e.Date <= dateTo)
            .ToListAsync(ct);

        // Approximate UTC boundaries for querying blocks and slots
        var defaultTz = TimeZoneHelper.ResolveTimeZone(courts.First().SportCenter!.TimeZoneId);
        var rangeStartUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(dateFrom.ToDateTime(TimeOnly.MinValue), defaultTz));
        var rangeEndUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(dateTo.ToDateTime(TimeOnly.MaxValue), defaultTz));

        var courtBlocks = await db.CourtBlocks.AsNoTracking()
            .Where(b => courtIds.Contains(b.CourtId) && b.StartAt < rangeEndUtc && b.EndAt > rangeStartUtc)
            .ToListAsync(ct);

        var bookingSlots = await db.BookingSlots.AsNoTracking()
            .Include(s => s.Booking)
            .Where(s => courtIds.Contains(s.CourtId) &&
                        s.StartAt < rangeEndUtc && s.EndAt > rangeStartUtc &&
                        s.IsOccupying &&
                        s.Booking!.BookingStatus != BookingStatus.Cancelled &&
                        s.Booking.BookingStatus != BookingStatus.Expired)
            .ToListAsync(ct);

        var courtStats = courts.ToDictionary(c => c.Id, c => new
        {
            Court = c,
            BookableMinutes = 0,
            OccupiedMinutes = 0
        });

        var courtBookable = courts.ToDictionary(c => c.Id, _ => 0);
        var courtOccupied = courts.ToDictionary(c => c.Id, _ => 0);

        var dayBookable = new Dictionary<DateOnly, int>();
        var dayOccupied = new Dictionary<DateOnly, int>();

        for (var currentDate = dateFrom; currentDate <= dateTo; currentDate = currentDate.AddDays(1))
        {
            var dayBookableMins = 0;
            var dayOccupiedMins = 0;

            foreach (var court in courts)
            {
                var tz = TimeZoneHelper.ResolveTimeZone(court.SportCenter!.TimeZoneId);
                var exc = exceptions.FirstOrDefault(e => e.SportCenterId == court.SportCenterId && e.Date == currentDate);

                bool isClosed;
                TimeOnly? open = null;
                TimeOnly? close = null;

                if (exc is not null)
                {
                    isClosed = exc.IsClosed;
                    open = exc.OpenTime;
                    close = exc.CloseTime;
                }
                else
                {
                    var dow = currentDate.DayOfWeek.ToCourtGoDayOfWeek();
                    var reg = operatingHours.FirstOrDefault(h => h.SportCenterId == court.SportCenterId && h.DayOfWeek == dow);
                    isClosed = reg is null || reg.IsClosed;
                    open = reg?.OpenTime;
                    close = reg?.CloseTime;
                }

                if (isClosed || open is null || close is null || open >= close)
                {
                    continue;
                }

                var dayStartUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(currentDate.ToDateTime(open.Value), tz));
                var dayEndUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(currentDate.ToDateTime(close.Value), tz));

                var openMinutes = (int)(close.Value - open.Value).TotalMinutes;

                // Subtract overlapping CourtBlocks
                var blocks = courtBlocks.Where(b => b.CourtId == court.Id && b.StartAt < dayEndUtc && b.EndAt > dayStartUtc);
                var blockMins = 0;
                foreach (var b in blocks)
                {
                    var overlapStart = b.StartAt > dayStartUtc ? b.StartAt : dayStartUtc;
                    var overlapEnd = b.EndAt < dayEndUtc ? b.EndAt : dayEndUtc;
                    if (overlapEnd > overlapStart)
                    {
                        blockMins += (int)(overlapEnd - overlapStart).TotalMinutes;
                    }
                }

                var netBookable = Math.Max(0, openMinutes - blockMins);
                courtBookable[court.Id] += netBookable;
                dayBookableMins += netBookable;

                // Sum occupying slots on this court overlapping with operating window
                var slots = bookingSlots.Where(s => s.CourtId == court.Id && s.StartAt < dayEndUtc && s.EndAt > dayStartUtc);
                var occMins = 0;
                foreach (var s in slots)
                {
                    var occStart = s.StartAt > dayStartUtc ? s.StartAt : dayStartUtc;
                    var occEnd = s.EndAt < dayEndUtc ? s.EndAt : dayEndUtc;
                    if (occEnd > occStart)
                    {
                        occMins += (int)(occEnd - occStart).TotalMinutes;
                    }
                }

                courtOccupied[court.Id] += occMins;
                dayOccupiedMins += occMins;
            }

            dayBookable[currentDate] = dayBookableMins;
            dayOccupied[currentDate] = dayOccupiedMins;
        }

        var totalBookable = courtBookable.Values.Sum();
        var totalOccupied = courtOccupied.Values.Sum();
        var overallRate = totalBookable > 0
            ? Math.Round((decimal)totalOccupied / totalBookable * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var byCourt = courts.Select(c =>
        {
            var bMins = courtBookable[c.Id];
            var oMins = courtOccupied[c.Id];
            var rate = bMins > 0
                ? Math.Round((decimal)oMins / bMins * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;
            return new AdminCourtOccupancyDto(
                c.Id,
                c.Name,
                c.Sport?.Name ?? string.Empty,
                bMins,
                oMins,
                rate
            );
        }).ToList();

        var byDay = dayBookable.Select(kvp =>
        {
            var bMins = kvp.Value;
            var oMins = dayOccupied[kvp.Key];
            var rate = bMins > 0
                ? Math.Round((decimal)oMins / bMins * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;
            return new AdminDayOccupancyDto(kvp.Key, bMins, oMins, rate);
        }).OrderBy(d => d.Date).ToList();

        return new AdminOccupancyReportResponse(
            dateFrom,
            dateTo,
            centerId,
            overallRate,
            totalBookable,
            totalOccupied,
            byCourt,
            byDay
        );
    }
}
