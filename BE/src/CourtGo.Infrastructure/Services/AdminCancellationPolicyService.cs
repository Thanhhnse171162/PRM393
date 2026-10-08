using CourtGo.Application.Cancellation;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class AdminCancellationPolicyService(
    CourtGoDbContext db,
    TimeProvider clock) : IAdminCancellationPolicyService
{
    public async Task<IReadOnlyList<AdminCancellationPolicySummaryDto>> GetPoliciesAsync(CancellationToken ct = default)
    {
        return await db.CancellationPolicies.AsNoTracking()
            .OrderByDescending(p => p.Version)
            .Select(p => new AdminCancellationPolicySummaryDto(
                p.Id,
                p.Name,
                p.Version,
                p.EffectiveFrom,
                p.EffectiveTo,
                p.IsActive,
                p.Rules.Count,
                p.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<AdminCancellationPolicyDetailDto> GetPolicyByIdAsync(Guid policyId, CancellationToken ct = default)
    {
        var policy = await db.CancellationPolicies.AsNoTracking()
            .Where(p => p.Id == policyId)
            .Select(p => new AdminCancellationPolicyDetailDto(
                p.Id,
                p.Name,
                p.Version,
                p.EffectiveFrom,
                p.EffectiveTo,
                p.IsActive,
                p.Rules.OrderBy(r => r.MinHoursBeforeStart)
                    .Select(r => new AdminCancellationPolicyRuleDto(r.Id, r.MinHoursBeforeStart, r.MaxHoursBeforeStart, r.RefundPercent))
                    .ToList(),
                p.CreatedAt))
            .SingleOrDefaultAsync(ct);

        if (policy is null)
            throw new NotFoundException($"Cancellation policy with ID '{policyId}' was not found.", ErrorCodes.PolicyNotFound);

        return policy;
    }

    public async Task<AdminCancellationPolicyDetailDto> CreatePolicyAsync(
        CreateCancellationPolicyRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Policy name is required.");
        var name = request.Name.Trim();
        if (name.Length > 150)
            throw new ValidationException("Policy name cannot exceed 150 characters.");

        if (request.Rules == null || request.Rules.Count == 0)
            throw new ValidationException("Policy must contain at least one cancellation rule.");

        // Validate each rule and check for range overlap
        var sortedRules = request.Rules.OrderBy(r => r.MinHoursBeforeStart).ToList();
        for (int i = 0; i < sortedRules.Count; i++)
        {
            var rule = sortedRules[i];
            if (rule.MinHoursBeforeStart < 0)
                throw new ValidationException("MinHoursBeforeStart cannot be negative.");

            if (rule.MaxHoursBeforeStart.HasValue && rule.MaxHoursBeforeStart.Value <= rule.MinHoursBeforeStart)
                throw new ValidationException("MaxHoursBeforeStart must be strictly greater than MinHoursBeforeStart.");

            if (rule.RefundPercent < 0 || rule.RefundPercent > 100)
                throw new ValidationException("RefundPercent must be between 0 and 100.");

            if (i < sortedRules.Count - 1)
            {
                var nextRule = sortedRules[i + 1];
                if (!rule.MaxHoursBeforeStart.HasValue)
                {
                    throw new ValidationException("Only the last rule can have an unbounded MaxHoursBeforeStart.");
                }
                if (rule.MaxHoursBeforeStart.Value > nextRule.MinHoursBeforeStart)
                {
                    throw new ValidationException(
                        $"Cancellation policy rules overlap: [{rule.MinHoursBeforeStart}, {rule.MaxHoursBeforeStart}] and [{nextRule.MinHoursBeforeStart}, {nextRule.MaxHoursBeforeStart}].");
                }
            }
        }

        var maxVersion = await db.CancellationPolicies.MaxAsync(p => (int?)p.Version, ct) ?? 0;
        var version = maxVersion + 1;

        var policy = new CancellationPolicy
        {
            Id = Guid.NewGuid(),
            Name = name,
            Version = version,
            EffectiveFrom = request.EffectiveFrom ?? clock.GetUtcNow(),
            EffectiveTo = null,
            IsActive = false,
            CreatedAt = clock.GetUtcNow()
        };

        foreach (var r in sortedRules)
        {
            policy.Rules.Add(new CancellationPolicyRule
            {
                Id = Guid.NewGuid(),
                CancellationPolicyId = policy.Id,
                MinHoursBeforeStart = r.MinHoursBeforeStart,
                MaxHoursBeforeStart = r.MaxHoursBeforeStart,
                RefundPercent = r.RefundPercent
            });
        }

        db.CancellationPolicies.Add(policy);
        await db.SaveChangesAsync(ct);

        return await GetPolicyByIdAsync(policy.Id, ct);
    }

    public async Task<AdminCancellationPolicyDetailDto> ActivatePolicyAsync(Guid policyId, CancellationToken ct = default)
    {
        var targetPolicy = await db.CancellationPolicies
            .Include(p => p.Rules)
            .FirstOrDefaultAsync(p => p.Id == policyId, ct);

        if (targetPolicy is null)
            throw new NotFoundException($"Cancellation policy with ID '{policyId}' was not found.", ErrorCodes.PolicyNotFound);

        if (targetPolicy.IsActive)
            return await GetPolicyByIdAsync(policyId, ct);

        var now = clock.GetUtcNow();

        // Deactivate currently active policy (one active policy filtered index UX_CancellationPolicies_OneActive)
        var currentlyActive = await db.CancellationPolicies
            .Where(p => p.IsActive && p.Id != policyId)
            .ToListAsync(ct);

        foreach (var active in currentlyActive)
        {
            active.IsActive = false;
            active.EffectiveTo = now;
        }

        targetPolicy.IsActive = true;
        if (targetPolicy.EffectiveFrom > now)
        {
            targetPolicy.EffectiveFrom = now;
        }
        targetPolicy.EffectiveTo = null;

        await db.SaveChangesAsync(ct);

        return await GetPolicyByIdAsync(policyId, ct);
    }
}
