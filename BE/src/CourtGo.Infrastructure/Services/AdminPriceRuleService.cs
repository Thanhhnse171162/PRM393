using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.PriceRules;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class AdminPriceRuleService(
    CourtGoDbContext db,
    TimeProvider clock) : IAdminPriceRuleService
{
    public async Task<IReadOnlyList<AdminPriceRuleDto>> GetPriceRulesAsync(
        Guid courtId,
        CourtGoDayOfWeek? dayOfWeek = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var courtExists = await db.Courts.AnyAsync(c => c.Id == courtId, ct);
        if (!courtExists)
            throw new NotFoundException($"Court with ID '{courtId}' was not found.", ErrorCodes.CourtNotFound);

        var query = db.PriceRules.AsNoTracking().Where(pr => pr.CourtId == courtId);

        if (dayOfWeek.HasValue)
            query = query.Where(pr => pr.DayOfWeek == dayOfWeek.Value);

        if (isActive.HasValue)
            query = query.Where(pr => pr.IsActive == isActive.Value);

        return await query
            .OrderBy(pr => pr.DayOfWeek)
            .ThenBy(pr => pr.StartTime)
            .ThenBy(pr => pr.EffectiveFrom)
            .Select(pr => new AdminPriceRuleDto(
                pr.Id,
                pr.CourtId,
                pr.DayOfWeek,
                pr.StartTime,
                pr.EndTime,
                pr.PricePerHour,
                pr.EffectiveFrom,
                pr.EffectiveTo,
                pr.IsActive,
                pr.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<AdminPriceRuleDto> GetPriceRuleByIdAsync(Guid courtId, Guid priceRuleId, CancellationToken ct = default)
    {
        var rule = await db.PriceRules.AsNoTracking()
            .Where(pr => pr.Id == priceRuleId && pr.CourtId == courtId)
            .Select(pr => new AdminPriceRuleDto(
                pr.Id,
                pr.CourtId,
                pr.DayOfWeek,
                pr.StartTime,
                pr.EndTime,
                pr.PricePerHour,
                pr.EffectiveFrom,
                pr.EffectiveTo,
                pr.IsActive,
                pr.CreatedAt))
            .SingleOrDefaultAsync(ct);

        if (rule is null)
            throw new NotFoundException($"Price rule with ID '{priceRuleId}' was not found.", ErrorCodes.PriceRuleNotFound);

        return rule;
    }

    public async Task<AdminPriceRuleDto> CreatePriceRuleAsync(
        Guid courtId,
        CreatePriceRuleRequest request,
        CancellationToken ct = default)
    {
        var courtExists = await db.Courts.AnyAsync(c => c.Id == courtId, ct);
        if (!courtExists)
            throw new NotFoundException($"Court with ID '{courtId}' was not found.", ErrorCodes.CourtNotFound);

        ValidatePriceRule(request.DayOfWeek, request.StartTime, request.EndTime, request.PricePerHour, request.EffectiveFrom, request.EffectiveTo);

        if (request.IsActive)
        {
            await ValidateNoOverlapAsync(courtId, null, request.DayOfWeek, request.StartTime, request.EndTime, request.EffectiveFrom, request.EffectiveTo, ct);
        }

        var rule = new PriceRule
        {
            Id = Guid.NewGuid(),
            CourtId = courtId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            PricePerHour = request.PricePerHour,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = request.IsActive,
            CreatedAt = clock.GetUtcNow()
        };

        db.PriceRules.Add(rule);
        await db.SaveChangesAsync(ct);

        return await GetPriceRuleByIdAsync(courtId, rule.Id, ct);
    }

    public async Task<AdminPriceRuleDto> UpdatePriceRuleAsync(
        Guid courtId,
        Guid priceRuleId,
        UpdatePriceRuleRequest request,
        CancellationToken ct = default)
    {
        var rule = await db.PriceRules.FirstOrDefaultAsync(pr => pr.Id == priceRuleId && pr.CourtId == courtId, ct);
        if (rule is null)
            throw new NotFoundException($"Price rule with ID '{priceRuleId}' was not found.", ErrorCodes.PriceRuleNotFound);

        ValidatePriceRule(request.DayOfWeek, request.StartTime, request.EndTime, request.PricePerHour, request.EffectiveFrom, request.EffectiveTo);

        if (request.IsActive)
        {
            await ValidateNoOverlapAsync(courtId, priceRuleId, request.DayOfWeek, request.StartTime, request.EndTime, request.EffectiveFrom, request.EffectiveTo, ct);
        }

        rule.DayOfWeek = request.DayOfWeek;
        rule.StartTime = request.StartTime;
        rule.EndTime = request.EndTime;
        rule.PricePerHour = request.PricePerHour;
        rule.EffectiveFrom = request.EffectiveFrom;
        rule.EffectiveTo = request.EffectiveTo;
        rule.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);

        return await GetPriceRuleByIdAsync(courtId, priceRuleId, ct);
    }

    public async Task<AdminPriceRuleDto> UpdatePriceRuleStatusAsync(
        Guid courtId,
        Guid priceRuleId,
        UpdatePriceRuleStatusRequest request,
        CancellationToken ct = default)
    {
        var rule = await db.PriceRules.FirstOrDefaultAsync(pr => pr.Id == priceRuleId && pr.CourtId == courtId, ct);
        if (rule is null)
            throw new NotFoundException($"Price rule with ID '{priceRuleId}' was not found.", ErrorCodes.PriceRuleNotFound);

        if (request.IsActive)
        {
            await ValidateNoOverlapAsync(courtId, priceRuleId, rule.DayOfWeek, rule.StartTime, rule.EndTime, rule.EffectiveFrom, rule.EffectiveTo, ct);
        }

        rule.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        return await GetPriceRuleByIdAsync(courtId, priceRuleId, ct);
    }

    private static void ValidatePriceRule(
        CourtGoDayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        decimal pricePerHour,
        DateOnly? effectiveFrom,
        DateOnly? effectiveTo)
    {
        if (!Enum.IsDefined(dayOfWeek))
            throw new ValidationException($"Invalid day of week: '{dayOfWeek}'.");

        if (pricePerHour < 0)
            throw new ValidationException("PricePerHour cannot be negative.");

        if (startTime >= endTime)
            throw new ValidationException("StartTime must be before EndTime.");

        if (effectiveFrom.HasValue && effectiveTo.HasValue && effectiveFrom.Value > effectiveTo.Value)
            throw new ValidationException("EffectiveFrom cannot be after EffectiveTo.");
    }

    private async Task ValidateNoOverlapAsync(
        Guid courtId,
        Guid? excludingRuleId,
        CourtGoDayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        DateOnly? effectiveFrom,
        DateOnly? effectiveTo,
        CancellationToken ct)
    {
        var existingActiveRules = await db.PriceRules.AsNoTracking()
            .Where(pr => pr.CourtId == courtId && pr.IsActive && pr.DayOfWeek == dayOfWeek)
            .Where(pr => !excludingRuleId.HasValue || pr.Id != excludingRuleId.Value)
            .ToListAsync(ct);

        foreach (var existing in existingActiveRules)
        {
            // Date overlap check:
            // Two intervals [A.From, A.To] and [B.From, B.To] overlap if A.From <= B.To && A.To >= B.From
            var datesOverlap = (existing.EffectiveTo == null || effectiveFrom == null || existing.EffectiveTo >= effectiveFrom)
                            && (existing.EffectiveFrom == null || effectiveTo == null || existing.EffectiveFrom <= effectiveTo);

            // Clock overlap check:
            // Two intervals [A.Start, A.End] and [B.Start, B.End] overlap if A.Start < B.End && A.End > B.Start
            var timesOverlap = existing.StartTime < endTime && existing.EndTime > startTime;

            if (datesOverlap && timesOverlap)
            {
                throw new ConflictException(
                    $"Active price rule overlaps with existing rule '{existing.Id}' ({existing.StartTime:HH\\:mm}-{existing.EndTime:HH\\:mm}).",
                    ErrorCodes.PriceRuleConflict);
            }
        }
    }
}
