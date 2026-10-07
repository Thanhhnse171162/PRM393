using CourtGo.Application.Availability;
using CourtGo.Application.Common;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CourtGo.Infrastructure.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly CourtGoDbContext _db;
    private readonly IExpiredBookingHoldService _expiredHoldService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(
        CourtGoDbContext db,
        IExpiredBookingHoldService expiredHoldService,
        TimeProvider timeProvider,
        ILogger<AvailabilityService> logger)
    {
        _db = db;
        _expiredHoldService = expiredHoldService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<CourtAvailabilityDto> GetCourtAvailabilityAsync(
        Guid courtId,
        DateOnly date,
        CancellationToken ct = default)
    {
        // 1. Court validation
        var court = await _db.Courts
            .AsNoTracking()
            .Include(c => c.Sport)
            .Include(c => c.SportCenter)
            .FirstOrDefaultAsync(c => c.Id == courtId, ct);

        if (court is null)
        {
            throw new NotFoundException($"Court with ID '{courtId}' was not found.", ErrorCodes.CourtNotFound);
        }

        if (court.SportCenter is null || court.SportCenter.Status != SportCenterStatus.Active)
        {
            throw new NotFoundException("Sport center is inactive or not found.", ErrorCodes.CourtNotFound);
        }

        if (court.Status == CourtStatus.Inactive)
        {
            throw new NotFoundException("Court is inactive.", ErrorCodes.CourtNotFound);
        }

        var courtDto = new AvailabilityCourtDto(
            court.Id,
            court.Code,
            court.Name,
            court.SportId,
            court.Sport?.Name ?? string.Empty,
            court.SportCenterId,
            court.SportCenter.Name
        );

        // 2. Timezone and Date Validation
        var tz = ResolveTimeZone(court.SportCenter.TimeZoneId);
        var currentUtc = _timeProvider.GetUtcNow();
        var currentCenterTime = TimeZoneInfo.ConvertTime(currentUtc, tz);
        var currentCenterDate = DateOnly.FromDateTime(currentCenterTime.DateTime);

        if (date < currentCenterDate)
        {
            throw new ValidationException("Requested date cannot be in the past.", ErrorCodes.InvalidAvailabilityDate);
        }

        // Maintenance court generates no available slots for customer booking
        if (court.Status == CourtStatus.Maintenance)
        {
            return new CourtAvailabilityDto(
                courtDto,
                date.ToString("yyyy-MM-dd"),
                IsClosed: false,
                OpeningHours: null,
                Slots: Array.Empty<AvailabilitySlotDto>()
            );
        }

        // 3. Release expired holds
        await _expiredHoldService.ReleaseExpiredHoldsAsync(ct);

        // 4. Operating hours & exception logic
        var exception = await _db.OperatingHourExceptions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.SportCenterId == court.SportCenterId && e.Date == date, ct);

        bool isClosed;
        TimeOnly? openTime = null;
        TimeOnly? closeTime = null;

        if (exception is not null)
        {
            if (exception.IsClosed)
            {
                isClosed = true;
            }
            else
            {
                isClosed = false;
                openTime = exception.OpenTime;
                closeTime = exception.CloseTime;
            }
        }
        else
        {
            var courtGoDay = date.DayOfWeek.ToCourtGoDayOfWeek();
            var regularHours = await _db.OperatingHours
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.SportCenterId == court.SportCenterId && o.DayOfWeek == courtGoDay, ct);

            if (regularHours is null || regularHours.IsClosed)
            {
                isClosed = true;
            }
            else
            {
                isClosed = false;
                openTime = regularHours.OpenTime;
                closeTime = regularHours.CloseTime;
            }
        }

        if (isClosed || openTime is null || closeTime is null || openTime >= closeTime)
        {
            return new CourtAvailabilityDto(
                courtDto,
                date.ToString("yyyy-MM-dd"),
                IsClosed: true,
                OpeningHours: null,
                Slots: Array.Empty<AvailabilitySlotDto>()
            );
        }

        // 5. Generate fixed 60-minute slots
        var rawSlots = new List<(DateTimeOffset StartAt, DateTimeOffset EndAt, TimeOnly SlotStart, TimeOnly SlotEnd)>();
        var slotCursor = openTime.Value;

        while (slotCursor.AddHours(1) <= closeTime.Value && slotCursor.AddHours(1) > slotCursor)
        {
            var nextSlot = slotCursor.AddHours(1);

            var localStartDt = date.ToDateTime(slotCursor);
            var localEndDt = date.ToDateTime(nextSlot);

            var startOffset = tz.GetUtcOffset(localStartDt);
            var endOffset = tz.GetUtcOffset(localEndDt);

            var startAt = new DateTimeOffset(localStartDt, startOffset);
            var endAt = new DateTimeOffset(localEndDt, endOffset);

            rawSlots.Add((startAt, endAt, slotCursor, nextSlot));

            slotCursor = nextSlot;
        }

        var openingHoursDto = new OpeningHoursDto(
            openTime.Value.ToString("HH:mm"),
            closeTime.Value.ToString("HH:mm")
        );

        if (rawSlots.Count == 0)
        {
            return new CourtAvailabilityDto(
                courtDto,
                date.ToString("yyyy-MM-dd"),
                IsClosed: false,
                OpeningHours: openingHoursDto,
                Slots: Array.Empty<AvailabilitySlotDto>()
            );
        }

        // 6. Past slot and lead time filtering for today
        if (date == currentCenterDate)
        {
            var settings = await _db.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == 1, ct);

            if (settings is null)
            {
                _logger.LogError("System settings row (Id = 1) is missing from database.");
                throw new ConfigurationException("System settings row (Id = 1) is missing.", ErrorCodes.ConfigurationNotFound);
            }

            var minAllowedStart = currentCenterTime.AddMinutes(settings.MinBookingLeadMinutes);
            rawSlots = rawSlots.Where(s => s.StartAt >= minAllowedStart).ToList();
        }

        if (rawSlots.Count == 0)
        {
            return new CourtAvailabilityDto(
                courtDto,
                date.ToString("yyyy-MM-dd"),
                IsClosed: false,
                OpeningHours: openingHoursDto,
                Slots: Array.Empty<AvailabilitySlotDto>()
            );
        }

        // 7. Load CourtBlocks, BookingSlots, and PriceRules for date window
        var windowStart = rawSlots.First().StartAt;
        var windowEnd = rawSlots.Last().EndAt;

        var blocks = await _db.CourtBlocks
            .AsNoTracking()
            .Where(b => b.CourtId == courtId && b.StartAt < windowEnd && b.EndAt > windowStart)
            .ToListAsync(ct);

        var bookingSlots = await _db.BookingSlots
            .AsNoTracking()
            .Where(bs => bs.CourtId == courtId && bs.StartAt < windowEnd && bs.EndAt > windowStart && bs.IsOccupying)
            .ToListAsync(ct);

        var dayOfWeekCourtGo = date.DayOfWeek.ToCourtGoDayOfWeek();
        var priceRules = await _db.PriceRules
            .AsNoTracking()
            .Where(pr => pr.CourtId == courtId && pr.IsActive && pr.DayOfWeek == dayOfWeekCourtGo)
            .Where(pr => pr.EffectiveFrom == null || date >= pr.EffectiveFrom)
            .Where(pr => pr.EffectiveTo == null || date <= pr.EffectiveTo)
            .ToListAsync(ct);

        // 8. Resolve Status and Price
        var finalSlots = new List<AvailabilitySlotDto>(rawSlots.Count);

        foreach (var slot in rawSlots)
        {
            // Block precedence
            bool isBlocked = blocks.Any(b => b.StartAt < slot.EndAt && b.EndAt > slot.StartAt);

            AvailabilitySlotStatus status;
            if (isBlocked)
            {
                status = AvailabilitySlotStatus.Blocked;
            }
            else
            {
                var occupyingSlot = bookingSlots.FirstOrDefault(bs =>
                    bs.StartAt < slot.EndAt && bs.EndAt > slot.StartAt);

                if (occupyingSlot is not null)
                {
                    if (occupyingSlot.ReservationState == ReservationState.Held)
                    {
                        // Defensively check against current UTC time
                        if (occupyingSlot.HoldExpiresAt.HasValue && occupyingSlot.HoldExpiresAt.Value > currentUtc)
                        {
                            status = AvailabilitySlotStatus.Held;
                        }
                        else
                        {
                            status = AvailabilitySlotStatus.Available;
                        }
                    }
                    else if (occupyingSlot.ReservationState == ReservationState.Reserved)
                    {
                        status = AvailabilitySlotStatus.Booked;
                    }
                    else
                    {
                        status = AvailabilitySlotStatus.Available;
                    }
                }
                else
                {
                    status = AvailabilitySlotStatus.Available;
                }
            }

            // Price resolution
            var matchingRules = priceRules
                .Where(r => r.StartTime <= slot.SlotStart && r.EndTime >= slot.SlotEnd)
                .ToList();

            if (matchingRules.Count > 1)
            {
                _logger.LogError("Multiple active price rules found for Court {CourtId} at slot {StartAt}", courtId, slot.StartAt);
                throw new ConfigurationException($"Multiple active price rules match court '{courtId}' at slot '{slot.StartAt}'.", ErrorCodes.ConfigurationError);
            }

            decimal price = matchingRules.Count == 1 ? matchingRules[0].PricePerHour : court.BasePricePerHour;

            finalSlots.Add(new AvailabilitySlotDto(
                slot.StartAt,
                slot.EndAt,
                price,
                status
            ));
        }

        return new CourtAvailabilityDto(
            courtDto,
            date.ToString("yyyy-MM-dd"),
            IsClosed: false,
            OpeningHours: openingHoursDto,
            Slots: finalSlots
        );
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            timeZoneId = "Asia/Ho_Chi_Minh";

        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var tz))
            return tz;

        if (string.Equals(timeZoneId, "Asia/Ho_Chi_Minh", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(timeZoneId, "Asia/Saigon", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(timeZoneId, "Asia/Bangkok", StringComparison.OrdinalIgnoreCase))
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById("SE Asia Standard Time", out var seAsia))
                return seAsia;
        }

        return TimeZoneInfo.CreateCustomTimeZone(timeZoneId, TimeSpan.FromHours(7), timeZoneId, timeZoneId);
    }
}
