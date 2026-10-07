using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Courts;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;

namespace CourtGo.Application.SportCenters;

public class SportCenterService : ISportCenterService
{
    private readonly ISportCenterRepository _repository;

    public SportCenterService(ISportCenterRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<SportCenterSummaryDto>> GetAllAsync(
        Guid? sportId = null,
        string? city = null,
        string? district = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var centers = await _repository.GetAllAsync(sportId, city, district, search, ct);

        return centers.Select(c =>
        {
            var activeCourts = c.Courts.Where(ct => ct.Status == CourtStatus.Active).ToList();
            var sports = activeCourts
                .Where(ct => ct.Sport is not null)
                .Select(ct => ct.Sport!.Name)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            decimal? priceFrom = activeCourts.Count > 0
                ? activeCourts.Min(ct => ct.BasePricePerHour)
                : null;

            return new SportCenterSummaryDto(
                c.Id,
                c.Name,
                c.AddressLine,
                c.Ward,
                c.District,
                c.City,
                c.Latitude,
                c.Longitude,
                c.PhoneNumber,
                c.CoverImageUrl,
                c.Status.ToString(),
                sports,
                priceFrom,
                activeCourts.Count
            );
        }).ToList();
    }

    public async Task<SportCenterDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var center = await _repository.GetByIdWithDetailsAsync(id, ct);
        if (center is null || center.Status != SportCenterStatus.Active)
        {
            throw new NotFoundException($"Sport center with ID '{id}' was not found.");
        }

        var activeCourts = center.Courts.Where(ct => ct.Status == CourtStatus.Active).ToList();
        var sports = activeCourts
            .Where(ct => ct.Sport is not null)
            .Select(ct => ct.Sport!.Name)
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        decimal? priceFrom = activeCourts.Count > 0
            ? activeCourts.Min(ct => ct.BasePricePerHour)
            : null;

        var operatingHours = center.OperatingHours
            .OrderBy(o => (int)o.DayOfWeek)
            .Select(o => new OperatingHourDto(
                o.DayOfWeek.ToString(),
                o.OpenTime,
                o.CloseTime,
                o.IsClosed
            ))
            .ToList();

        var courtDtos = activeCourts
            .OrderBy(ct => ct.Name)
            .Select(ct => new CourtDto(
                ct.Id,
                center.Id,
                center.Name,
                ct.SportId,
                ct.Sport?.Name ?? string.Empty,
                ct.Code,
                ct.Name,
                ct.SurfaceType,
                ct.Description,
                ct.CoverImageUrl,
                ct.BasePricePerHour,
                ct.Status.ToString()
            ))
            .ToList();

        return new SportCenterDetailDto(
            center.Id,
            center.Name,
            center.AddressLine,
            center.Ward,
            center.District,
            center.City,
            center.Latitude,
            center.Longitude,
            center.TimeZoneId,
            center.PhoneNumber,
            center.CoverImageUrl,
            center.Status.ToString(),
            sports,
            priceFrom,
            activeCourts.Count,
            operatingHours,
            courtDtos
        );
    }
}
