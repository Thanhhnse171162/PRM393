using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Sports;
using CourtGo.Domain.Entities;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class AdminSportService(
    CourtGoDbContext db,
    TimeProvider clock) : IAdminSportService
{
    public async Task<PagedResult<AdminSportDto>> GetSportsAsync(
        AdminSportQuery query,
        CancellationToken ct = default)
    {
        StaffOperationsService.ValidatePage(query.PageNumber, query.PageSize);

        var querySports = db.Sports.AsNoTracking().AsQueryable();

        if (query.IsActive.HasValue)
        {
            querySports = querySports.Where(s => s.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (search.Length > 200)
                throw new ValidationException("Search exceeds 200 characters.");

            querySports = querySports.Where(s => s.Name.Contains(search) || s.Code.Contains(search));
        }

        var total = await querySports.CountAsync(ct);

        var items = await querySports
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new AdminSportDto(
                s.Id,
                s.Code,
                s.Name,
                s.IconUrl,
                s.DisplayOrder,
                s.IsActive,
                s.Courts.Count,
                s.CreatedAt))
            .ToListAsync(ct);

        return new(items, query.PageNumber, query.PageSize, total, (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public async Task<AdminSportDetailDto> GetSportByIdAsync(Guid sportId, CancellationToken ct = default)
    {
        var sport = await db.Sports.AsNoTracking()
            .Where(s => s.Id == sportId)
            .Select(s => new AdminSportDetailDto(
                s.Id,
                s.Code,
                s.Name,
                s.IconUrl,
                s.DisplayOrder,
                s.IsActive,
                s.Courts.Count,
                s.CreatedAt))
            .SingleOrDefaultAsync(ct);

        if (sport is null)
            throw new NotFoundException($"Sport with ID '{sportId}' was not found.", ErrorCodes.SportNotFound);

        return sport;
    }

    public async Task<AdminSportDetailDto> CreateSportAsync(CreateSportRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Sport name is required.");

        var name = request.Name.Trim();
        if (name.Length > 100)
            throw new ValidationException("Sport name cannot exceed 100 characters.");

        string code;
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            code = request.Code.Trim();
            if (code.Length > 30)
                throw new ValidationException("Sport code cannot exceed 30 characters.");
        }
        else
        {
            code = GenerateCodeFromName(name);
        }

        if (request.DisplayOrder < 0)
            throw new ValidationException("Display order cannot be negative.");

        var iconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl.Trim();
        if (iconUrl?.Length > 500)
            throw new ValidationException("Icon URL cannot exceed 500 characters.");

        var duplicateCode = await db.Sports.AnyAsync(s => s.Code.ToLower() == code.ToLower(), ct);
        if (duplicateCode)
            throw new ConflictException($"Sport with code '{code}' already exists.", ErrorCodes.DuplicateSport);

        var duplicateName = await db.Sports.AnyAsync(s => s.Name.ToLower() == name.ToLower(), ct);
        if (duplicateName)
            throw new ConflictException($"Sport with name '{name}' already exists.", ErrorCodes.DuplicateSport);

        var sport = new Sport
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            IconUrl = iconUrl,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedAt = clock.GetUtcNow()
        };

        db.Sports.Add(sport);
        await db.SaveChangesAsync(ct);

        return await GetSportByIdAsync(sport.Id, ct);
    }

    public async Task<AdminSportDetailDto> UpdateSportAsync(
        Guid sportId,
        UpdateSportRequest request,
        CancellationToken ct = default)
    {
        var sport = await db.Sports.FirstOrDefaultAsync(s => s.Id == sportId, ct);
        if (sport is null)
            throw new NotFoundException($"Sport with ID '{sportId}' was not found.", ErrorCodes.SportNotFound);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Sport name is required.");

        var name = request.Name.Trim();
        if (name.Length > 100)
            throw new ValidationException("Sport name cannot exceed 100 characters.");

        if (request.DisplayOrder < 0)
            throw new ValidationException("Display order cannot be negative.");

        var iconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl.Trim();
        if (iconUrl?.Length > 500)
            throw new ValidationException("Icon URL cannot exceed 500 characters.");

        var duplicateName = await db.Sports.AnyAsync(s => s.Id != sportId && s.Name.ToLower() == name.ToLower(), ct);
        if (duplicateName)
            throw new ConflictException($"Sport with name '{name}' already exists.", ErrorCodes.DuplicateSport);

        sport.Name = name;
        sport.IconUrl = iconUrl;
        sport.DisplayOrder = request.DisplayOrder;

        await db.SaveChangesAsync(ct);

        return await GetSportByIdAsync(sport.Id, ct);
    }

    public async Task<AdminSportDetailDto> UpdateSportStatusAsync(
        Guid sportId,
        UpdateSportStatusRequest request,
        CancellationToken ct = default)
    {
        var sport = await db.Sports.FirstOrDefaultAsync(s => s.Id == sportId, ct);
        if (sport is null)
            throw new NotFoundException($"Sport with ID '{sportId}' was not found.", ErrorCodes.SportNotFound);

        sport.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        return await GetSportByIdAsync(sport.Id, ct);
    }

    private static string GenerateCodeFromName(string name)
    {
        var slug = new string(name.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray());
        slug = slug.Replace(' ', '-');
        if (slug.Length > 30)
            slug = slug[..30].TrimEnd('-');
        return string.IsNullOrWhiteSpace(slug) ? "sport-" + Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
