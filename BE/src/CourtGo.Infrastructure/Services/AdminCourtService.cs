using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Courts;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class AdminCourtService(
    CourtGoDbContext db,
    TimeProvider clock) : IAdminCourtService
{
    public async Task<PagedResult<AdminCourtListItemDto>> GetCourtsAsync(
        AdminCourtQuery query,
        CancellationToken ct = default)
    {
        StaffOperationsService.ValidatePage(query.PageNumber, query.PageSize);

        var queryCourts = db.Courts.AsNoTracking().AsQueryable();

        if (query.CenterId.HasValue)
        {
            queryCourts = queryCourts.Where(c => c.SportCenterId == query.CenterId.Value);
        }

        if (query.SportId.HasValue)
        {
            queryCourts = queryCourts.Where(c => c.SportId == query.SportId.Value);
        }

        if (query.Status.HasValue)
        {
            queryCourts = queryCourts.Where(c => c.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (search.Length > 200)
                throw new ValidationException("Search exceeds 200 characters.");

            queryCourts = queryCourts.Where(c => c.Code.Contains(search) || c.Name.Contains(search));
        }

        var total = await queryCourts.CountAsync(ct);

        var items = await queryCourts
            .OrderBy(c => c.SportCenter!.Name)
            .ThenBy(c => c.Code)
            .ThenBy(c => c.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new AdminCourtListItemDto(
                c.Id,
                c.SportCenterId,
                c.SportCenter!.Name,
                c.SportId,
                c.Sport!.Name,
                c.Code,
                c.Name,
                c.SurfaceType,
                c.BasePricePerHour,
                c.Status.ToString(),
                c.CoverImageUrl,
                c.CreatedAt))
            .ToListAsync(ct);

        return new(items, query.PageNumber, query.PageSize, total, (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public async Task<AdminCourtDetailDto> GetCourtByIdAsync(Guid courtId, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();

        var court = await db.Courts.AsNoTracking()
            .Where(c => c.Id == courtId)
            .Select(c => new AdminCourtDetailDto(
                c.Id,
                c.SportCenterId,
                c.SportCenter!.Name,
                c.SportId,
                c.Sport!.Name,
                c.Code,
                c.Name,
                c.SurfaceType,
                c.Description,
                c.CoverImageUrl,
                c.BasePricePerHour,
                c.Status.ToString(),
                c.Bookings.Count(b => b.EndAt > now && (b.BookingStatus == BookingStatus.Confirmed || b.BookingStatus == BookingStatus.CheckedIn || b.BookingStatus == BookingStatus.InProgress)),
                c.CreatedAt,
                c.UpdatedAt))
            .SingleOrDefaultAsync(ct);

        if (court is null)
            throw new NotFoundException($"Court with ID '{courtId}' was not found.", ErrorCodes.CourtNotFound);

        return court;
    }

    public async Task<AdminCourtDetailDto> CreateCourtAsync(CreateCourtRequest request, CancellationToken ct = default)
    {
        var center = await db.SportCenters.AsNoTracking().FirstOrDefaultAsync(sc => sc.Id == request.SportCenterId, ct);
        if (center is null)
            throw new NotFoundException($"Sport center with ID '{request.SportCenterId}' was not found.", ErrorCodes.SportCenterNotFound);
        if (center.Status != SportCenterStatus.Active)
            throw new ValidationException("Target sport center is not active.");

        var sport = await db.Sports.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.SportId, ct);
        if (sport is null)
            throw new NotFoundException($"Sport with ID '{request.SportId}' was not found.", ErrorCodes.SportNotFound);
        if (!sport.IsActive)
            throw new ValidationException("Target sport is not active.");

        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ValidationException("Court code is required.");
        var code = request.Code.Trim();
        if (code.Length > 50)
            throw new ValidationException("Court code cannot exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Court name is required.");
        var name = request.Name.Trim();
        if (name.Length > 100)
            throw new ValidationException("Court name cannot exceed 100 characters.");

        if (request.BasePricePerHour < 0)
            throw new ValidationException("Base price per hour cannot be negative.");

        var surfaceType = string.IsNullOrWhiteSpace(request.SurfaceType) ? null : request.SurfaceType.Trim();
        if (surfaceType?.Length > 100)
            throw new ValidationException("Surface type cannot exceed 100 characters.");

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (description?.Length > 500)
            throw new ValidationException("Description cannot exceed 500 characters.");

        var coverImageUrl = string.IsNullOrWhiteSpace(request.CoverImageUrl) ? null : request.CoverImageUrl.Trim();
        if (coverImageUrl?.Length > 500)
            throw new ValidationException("Cover image URL cannot exceed 500 characters.");

        var duplicateCode = await db.Courts.AnyAsync(
            c => c.SportCenterId == request.SportCenterId && c.Code.ToLower() == code.ToLower(), ct);
        if (duplicateCode)
            throw new ConflictException($"Court code '{code}' already exists in this sport center.", ErrorCodes.DuplicateCourtCode);

        var court = new Court
        {
            Id = Guid.NewGuid(),
            SportCenterId = request.SportCenterId,
            SportId = request.SportId,
            Code = code,
            Name = name,
            SurfaceType = surfaceType,
            Description = description,
            CoverImageUrl = coverImageUrl,
            BasePricePerHour = request.BasePricePerHour,
            Status = CourtStatus.Active,
            CreatedAt = clock.GetUtcNow()
        };

        db.Courts.Add(court);
        await db.SaveChangesAsync(ct);

        return await GetCourtByIdAsync(court.Id, ct);
    }

    public async Task<AdminCourtDetailDto> UpdateCourtAsync(
        Guid courtId,
        UpdateCourtRequest request,
        CancellationToken ct = default)
    {
        var court = await db.Courts.FirstOrDefaultAsync(c => c.Id == courtId, ct);
        if (court is null)
            throw new NotFoundException($"Court with ID '{courtId}' was not found.", ErrorCodes.CourtNotFound);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Court name is required.");
        var name = request.Name.Trim();
        if (name.Length > 100)
            throw new ValidationException("Court name cannot exceed 100 characters.");

        if (request.BasePricePerHour < 0)
            throw new ValidationException("Base price per hour cannot be negative.");

        var surfaceType = string.IsNullOrWhiteSpace(request.SurfaceType) ? null : request.SurfaceType.Trim();
        if (surfaceType?.Length > 100)
            throw new ValidationException("Surface type cannot exceed 100 characters.");

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (description?.Length > 500)
            throw new ValidationException("Description cannot exceed 500 characters.");

        var coverImageUrl = string.IsNullOrWhiteSpace(request.CoverImageUrl) ? null : request.CoverImageUrl.Trim();
        if (coverImageUrl?.Length > 500)
            throw new ValidationException("Cover image URL cannot exceed 500 characters.");

        if (request.SportId.HasValue && request.SportId.Value != court.SportId)
        {
            var hasBookings = await db.Bookings.AnyAsync(b => b.CourtId == courtId, ct);
            if (hasBookings)
                throw new ConflictException("Cannot change sport for a court with booking history.", ErrorCodes.CourtNotEditable);

            var newSport = await db.Sports.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.SportId.Value, ct);
            if (newSport is null)
                throw new NotFoundException($"Sport with ID '{request.SportId.Value}' was not found.", ErrorCodes.SportNotFound);
            if (!newSport.IsActive)
                throw new ValidationException("Target sport is not active.");

            court.SportId = request.SportId.Value;
        }

        court.Name = name;
        court.SurfaceType = surfaceType;
        court.Description = description;
        court.CoverImageUrl = coverImageUrl;
        court.BasePricePerHour = request.BasePricePerHour;
        court.UpdatedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);

        return await GetCourtByIdAsync(court.Id, ct);
    }

    public async Task<AdminCourtDetailDto> UpdateCourtStatusAsync(
        Guid courtId,
        UpdateCourtStatusRequest request,
        CancellationToken ct = default)
    {
        var court = await db.Courts.FirstOrDefaultAsync(c => c.Id == courtId, ct);
        if (court is null)
            throw new NotFoundException($"Court with ID '{courtId}' was not found.", ErrorCodes.CourtNotFound);

        if (!Enum.IsDefined(request.Status))
            throw new ValidationException("Invalid court status.");

        court.Status = request.Status;
        court.UpdatedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);

        return await GetCourtByIdAsync(court.Id, ct);
    }
}
