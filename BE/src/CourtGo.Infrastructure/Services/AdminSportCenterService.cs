using System.Text.RegularExpressions;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.SportCenters;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public partial class AdminSportCenterService(
    CourtGoDbContext db,
    TimeProvider clock) : IAdminSportCenterService
{
    private static readonly Regex PhoneRegex = new(@"^\+?[0-9]{9,15}$", RegexOptions.Compiled);

    public async Task<PagedResult<AdminSportCenterListItemDto>> GetSportCentersAsync(
        AdminSportCenterQuery query,
        CancellationToken ct = default)
    {
        StaffOperationsService.ValidatePage(query.PageNumber, query.PageSize);

        var queryCenters = db.SportCenters.AsNoTracking().AsQueryable();

        if (query.Status.HasValue)
        {
            queryCenters = queryCenters.Where(c => c.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            queryCenters = queryCenters.Where(c => c.City.Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(query.District))
        {
            var district = query.District.Trim();
            queryCenters = queryCenters.Where(c => c.District.Contains(district));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (search.Length > 200)
                throw new ValidationException("Search exceeds 200 characters.");

            queryCenters = queryCenters.Where(c =>
                c.Name.Contains(search) ||
                c.AddressLine.Contains(search) ||
                c.City.Contains(search) ||
                c.District.Contains(search) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(search)));
        }

        var total = await queryCenters.CountAsync(ct);

        var items = await queryCenters
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new AdminSportCenterListItemDto(
                c.Id,
                c.Name,
                c.AddressLine,
                c.City,
                c.District,
                c.PhoneNumber,
                c.Latitude,
                c.Longitude,
                c.TimeZoneId,
                c.Status.ToString(),
                c.Courts.Count,
                c.Courts.Count(ct => ct.Status == CourtStatus.Active),
                c.CreatedAt))
            .ToListAsync(ct);

        return new(items, query.PageNumber, query.PageSize, total, (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public async Task<AdminSportCenterDetailDto> GetSportCenterByIdAsync(Guid centerId, CancellationToken ct = default)
    {
        var center = await db.SportCenters.AsNoTracking()
            .Where(c => c.Id == centerId)
            .Select(c => new AdminSportCenterDetailDto(
                c.Id,
                c.Name,
                c.AddressLine,
                c.Ward,
                c.District,
                c.City,
                c.Latitude,
                c.Longitude,
                c.TimeZoneId,
                c.PhoneNumber,
                c.CoverImageUrl,
                c.Status.ToString(),
                c.Courts.Count,
                c.Courts.Count(ct => ct.Status == CourtStatus.Active),
                c.StaffAssignments.Count(sa => sa.IsActive),
                c.CreatedAt,
                c.UpdatedAt))
            .SingleOrDefaultAsync(ct);

        if (center is null)
            throw new NotFoundException($"Sport center with ID '{centerId}' was not found.", ErrorCodes.SportCenterNotFound);

        return center;
    }

    public async Task<AdminSportCenterDetailDto> CreateSportCenterAsync(
        CreateSportCenterRequest request,
        CancellationToken ct = default)
    {
        ValidateCenterInput(
            request.Name,
            request.AddressLine,
            request.Ward,
            request.District,
            request.City,
            request.Latitude,
            request.Longitude,
            request.TimeZoneId,
            request.PhoneNumber,
            request.CoverImageUrl,
            out var name,
            out var addressLine,
            out var ward,
            out var district,
            out var city,
            out var timeZoneId,
            out var phone,
            out var coverImageUrl);

        var center = new SportCenter
        {
            Id = Guid.NewGuid(),
            Name = name,
            AddressLine = addressLine,
            Ward = ward,
            District = district,
            City = city,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TimeZoneId = timeZoneId,
            PhoneNumber = phone,
            CoverImageUrl = coverImageUrl,
            Status = SportCenterStatus.Active,
            CreatedAt = clock.GetUtcNow()
        };

        db.SportCenters.Add(center);
        await db.SaveChangesAsync(ct);

        return await GetSportCenterByIdAsync(center.Id, ct);
    }

    public async Task<AdminSportCenterDetailDto> UpdateSportCenterAsync(
        Guid centerId,
        UpdateSportCenterRequest request,
        CancellationToken ct = default)
    {
        var center = await db.SportCenters.FirstOrDefaultAsync(c => c.Id == centerId, ct);
        if (center is null)
            throw new NotFoundException($"Sport center with ID '{centerId}' was not found.", ErrorCodes.SportCenterNotFound);

        ValidateCenterInput(
            request.Name,
            request.AddressLine,
            request.Ward,
            request.District,
            request.City,
            request.Latitude,
            request.Longitude,
            request.TimeZoneId,
            request.PhoneNumber,
            request.CoverImageUrl,
            out var name,
            out var addressLine,
            out var ward,
            out var district,
            out var city,
            out var timeZoneId,
            out var phone,
            out var coverImageUrl);

        center.Name = name;
        center.AddressLine = addressLine;
        center.Ward = ward;
        center.District = district;
        center.City = city;
        center.Latitude = request.Latitude;
        center.Longitude = request.Longitude;
        center.TimeZoneId = timeZoneId;
        center.PhoneNumber = phone;
        center.CoverImageUrl = coverImageUrl;
        center.UpdatedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);

        return await GetSportCenterByIdAsync(center.Id, ct);
    }

    public async Task<AdminSportCenterDetailDto> UpdateSportCenterStatusAsync(
        Guid centerId,
        UpdateSportCenterStatusRequest request,
        CancellationToken ct = default)
    {
        var center = await db.SportCenters.FirstOrDefaultAsync(c => c.Id == centerId, ct);
        if (center is null)
            throw new NotFoundException($"Sport center with ID '{centerId}' was not found.", ErrorCodes.SportCenterNotFound);

        if (!Enum.IsDefined(request.Status))
            throw new ValidationException("Invalid sport center status.");

        center.Status = request.Status;
        center.UpdatedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);

        return await GetSportCenterByIdAsync(center.Id, ct);
    }

    private static void ValidateCenterInput(
        string? rawName,
        string? rawAddressLine,
        string? rawWard,
        string? rawDistrict,
        string? rawCity,
        decimal? latitude,
        decimal? longitude,
        string? rawTimeZoneId,
        string? rawPhoneNumber,
        string? rawCoverImageUrl,
        out string name,
        out string addressLine,
        out string? ward,
        out string district,
        out string city,
        out string timeZoneId,
        out string? phone,
        out string? coverImageUrl)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            throw new ValidationException("Sport center name is required.");
        name = rawName.Trim();
        if (name.Length > 200)
            throw new ValidationException("Sport center name cannot exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(rawAddressLine))
            throw new ValidationException("Address line is required.");
        addressLine = rawAddressLine.Trim();
        if (addressLine.Length > 300)
            throw new ValidationException("Address line cannot exceed 300 characters.");

        ward = string.IsNullOrWhiteSpace(rawWard) ? null : rawWard.Trim();
        if (ward?.Length > 100)
            throw new ValidationException("Ward cannot exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(rawDistrict))
            throw new ValidationException("District is required.");
        district = rawDistrict.Trim();
        if (district.Length > 100)
            throw new ValidationException("District cannot exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(rawCity))
            throw new ValidationException("City is required.");
        city = rawCity.Trim();
        if (city.Length > 100)
            throw new ValidationException("City cannot exceed 100 characters.");

        if (latitude.HasValue && (latitude.Value < -90m || latitude.Value > 90m))
            throw new ValidationException("Latitude must be between -90 and 90 degrees.");

        if (longitude.HasValue && (longitude.Value < -180m || longitude.Value > 180m))
            throw new ValidationException("Longitude must be between -180 and 180 degrees.");

        timeZoneId = string.IsNullOrWhiteSpace(rawTimeZoneId)
            ? TimeZoneHelper.DefaultTimeZoneId
            : rawTimeZoneId.Trim();

        if (timeZoneId.Length > 64)
            throw new ValidationException("TimeZoneId cannot exceed 64 characters.");

        if (!TimeZoneHelper.IsValidTimeZone(timeZoneId))
            throw new ValidationException($"Invalid timezone identifier: '{timeZoneId}'.", ErrorCodes.InvalidTimezone);

        phone = string.IsNullOrWhiteSpace(rawPhoneNumber) ? null : rawPhoneNumber.Trim();
        if (phone != null)
        {
            if (phone.Length > 20)
                throw new ValidationException("Phone number cannot exceed 20 characters.");
            if (!PhoneRegex.IsMatch(phone))
                throw new ValidationException("Invalid phone number format.");
        }

        coverImageUrl = string.IsNullOrWhiteSpace(rawCoverImageUrl) ? null : rawCoverImageUrl.Trim();
        if (coverImageUrl?.Length > 500)
            throw new ValidationException("Cover image URL cannot exceed 500 characters.");
    }
}
