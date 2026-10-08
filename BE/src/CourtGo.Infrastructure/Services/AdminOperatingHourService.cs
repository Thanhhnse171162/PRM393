using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.OperatingHours;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class AdminOperatingHourService(CourtGoDbContext db) : IAdminOperatingHourService
{
    public async Task<AdminCenterOperatingHoursDto> GetOperatingHoursAsync(Guid centerId, CancellationToken ct = default)
    {
        var center = await db.SportCenters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == centerId, ct);
        if (center is null)
            throw new NotFoundException($"Sport center with ID '{centerId}' was not found.", ErrorCodes.SportCenterNotFound);

        var existing = await db.OperatingHours.AsNoTracking()
            .Where(o => o.SportCenterId == centerId)
            .ToListAsync(ct);

        var days = new List<AdminOperatingHourItemDto>(7);
        for (int i = 1; i <= 7; i++)
        {
            var dayEnum = (CourtGoDayOfWeek)i;
            var row = existing.FirstOrDefault(o => o.DayOfWeek == dayEnum);
            if (row != null)
            {
                days.Add(new AdminOperatingHourItemDto(row.DayOfWeek, row.IsClosed, row.OpenTime, row.CloseTime));
            }
            else
            {
                // Default: open 07:00 to 22:00 if not configured yet
                days.Add(new AdminOperatingHourItemDto(dayEnum, false, new TimeOnly(7, 0), new TimeOnly(22, 0)));
            }
        }

        return new AdminCenterOperatingHoursDto(center.Id, center.TimeZoneId, days);
    }

    public async Task<AdminCenterOperatingHoursDto> UpdateOperatingHoursAsync(
        Guid centerId,
        UpdateOperatingHoursRequest request,
        CancellationToken ct = default)
    {
        var center = await db.SportCenters.FirstOrDefaultAsync(c => c.Id == centerId, ct);
        if (center is null)
            throw new NotFoundException($"Sport center with ID '{centerId}' was not found.", ErrorCodes.SportCenterNotFound);

        if (request.Days == null || request.Days.Count == 0)
            throw new ValidationException("Days list cannot be empty.");

        if (request.Days.GroupBy(d => d.DayOfWeek).Any(g => g.Count() > 1))
            throw new ValidationException("Duplicate day of week in request.");

        foreach (var item in request.Days)
        {
            if (!Enum.IsDefined(item.DayOfWeek))
                throw new ValidationException($"Invalid day of week: '{item.DayOfWeek}'.");

            if (!item.IsClosed)
            {
                if (item.OpenTime == null || item.CloseTime == null)
                    throw new ValidationException($"OpenTime and CloseTime are required when {item.DayOfWeek} is not closed.");
                if (item.OpenTime >= item.CloseTime)
                    throw new ValidationException($"OpenTime must be before CloseTime for {item.DayOfWeek}.");
            }
        }

        var existing = await db.OperatingHours
            .Where(o => o.SportCenterId == centerId)
            .ToListAsync(ct);

        foreach (var item in request.Days)
        {
            var openTime = item.IsClosed ? null : item.OpenTime;
            var closeTime = item.IsClosed ? null : item.CloseTime;

            var match = existing.FirstOrDefault(o => o.DayOfWeek == item.DayOfWeek);
            if (match != null)
            {
                match.IsClosed = item.IsClosed;
                match.OpenTime = openTime;
                match.CloseTime = closeTime;
            }
            else
            {
                db.OperatingHours.Add(new OperatingHour
                {
                    Id = Guid.NewGuid(),
                    SportCenterId = centerId,
                    DayOfWeek = item.DayOfWeek,
                    IsClosed = item.IsClosed,
                    OpenTime = openTime,
                    CloseTime = closeTime
                });
            }
        }

        await db.SaveChangesAsync(ct);

        return await GetOperatingHoursAsync(centerId, ct);
    }

    public async Task<IReadOnlyList<AdminOperatingHourExceptionDto>> GetOperatingHourExceptionsAsync(
        Guid centerId,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default)
    {
        var centerExists = await db.SportCenters.AnyAsync(c => c.Id == centerId, ct);
        if (!centerExists)
            throw new NotFoundException($"Sport center with ID '{centerId}' was not found.", ErrorCodes.SportCenterNotFound);

        var query = db.OperatingHourExceptions.AsNoTracking().Where(e => e.SportCenterId == centerId);

        if (from.HasValue)
            query = query.Where(e => e.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.Date <= to.Value);

        return await query
            .OrderBy(e => e.Date)
            .Select(e => new AdminOperatingHourExceptionDto(
                e.Id,
                e.SportCenterId,
                e.Date,
                e.IsClosed,
                e.OpenTime,
                e.CloseTime,
                e.Reason))
            .ToListAsync(ct);
    }

    public async Task<AdminOperatingHourExceptionDto> CreateOperatingHourExceptionAsync(
        Guid centerId,
        CreateOperatingHourExceptionRequest request,
        CancellationToken ct = default)
    {
        var centerExists = await db.SportCenters.AnyAsync(c => c.Id == centerId, ct);
        if (!centerExists)
            throw new NotFoundException($"Sport center with ID '{centerId}' was not found.", ErrorCodes.SportCenterNotFound);

        var duplicate = await db.OperatingHourExceptions.AnyAsync(
            e => e.SportCenterId == centerId && e.Date == request.Date, ct);
        if (duplicate)
            throw new ConflictException($"An operating hour exception already exists for date {request.Date:yyyy-MM-dd}.", ErrorCodes.Conflict);

        if (!request.IsClosed)
        {
            if (request.OpenTime == null || request.CloseTime == null)
                throw new ValidationException("OpenTime and CloseTime are required when center is not closed.");
            if (request.OpenTime >= request.CloseTime)
                throw new ValidationException("OpenTime must be before CloseTime.");
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        if (reason?.Length > 300)
            throw new ValidationException("Reason cannot exceed 300 characters.");

        var exception = new OperatingHourException
        {
            Id = Guid.NewGuid(),
            SportCenterId = centerId,
            Date = request.Date,
            IsClosed = request.IsClosed,
            OpenTime = request.IsClosed ? null : request.OpenTime,
            CloseTime = request.IsClosed ? null : request.CloseTime,
            Reason = reason
        };

        db.OperatingHourExceptions.Add(exception);
        await db.SaveChangesAsync(ct);

        return new AdminOperatingHourExceptionDto(
            exception.Id,
            exception.SportCenterId,
            exception.Date,
            exception.IsClosed,
            exception.OpenTime,
            exception.CloseTime,
            exception.Reason);
    }

    public async Task<AdminOperatingHourExceptionDto> UpdateOperatingHourExceptionAsync(
        Guid centerId,
        Guid exceptionId,
        UpdateOperatingHourExceptionRequest request,
        CancellationToken ct = default)
    {
        var exception = await db.OperatingHourExceptions
            .FirstOrDefaultAsync(e => e.Id == exceptionId && e.SportCenterId == centerId, ct);
        if (exception is null)
            throw new NotFoundException($"Operating hour exception with ID '{exceptionId}' was not found.", ErrorCodes.OperatingHourExceptionNotFound);

        if (!request.IsClosed)
        {
            if (request.OpenTime == null || request.CloseTime == null)
                throw new ValidationException("OpenTime and CloseTime are required when center is not closed.");
            if (request.OpenTime >= request.CloseTime)
                throw new ValidationException("OpenTime must be before CloseTime.");
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        if (reason?.Length > 300)
            throw new ValidationException("Reason cannot exceed 300 characters.");

        exception.IsClosed = request.IsClosed;
        exception.OpenTime = request.IsClosed ? null : request.OpenTime;
        exception.CloseTime = request.IsClosed ? null : request.CloseTime;
        exception.Reason = reason;

        await db.SaveChangesAsync(ct);

        return new AdminOperatingHourExceptionDto(
            exception.Id,
            exception.SportCenterId,
            exception.Date,
            exception.IsClosed,
            exception.OpenTime,
            exception.CloseTime,
            exception.Reason);
    }

    public async Task DeleteOperatingHourExceptionAsync(
        Guid centerId,
        Guid exceptionId,
        CancellationToken ct = default)
    {
        var exception = await db.OperatingHourExceptions
            .FirstOrDefaultAsync(e => e.Id == exceptionId && e.SportCenterId == centerId, ct);
        if (exception is null)
            throw new NotFoundException($"Operating hour exception with ID '{exceptionId}' was not found.", ErrorCodes.OperatingHourExceptionNotFound);

        db.OperatingHourExceptions.Remove(exception);
        await db.SaveChangesAsync(ct);
    }
}
