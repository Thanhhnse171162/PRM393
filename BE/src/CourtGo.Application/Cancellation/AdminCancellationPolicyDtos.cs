using System.Text.Json.Serialization;

namespace CourtGo.Application.Cancellation;

public record AdminCancellationPolicySummaryDto(
    Guid Id,
    string Name,
    int Version,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    bool IsActive,
    int RuleCount,
    DateTimeOffset CreatedAt);

public record AdminCancellationPolicyDetailDto(
    Guid Id,
    string Name,
    int Version,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    bool IsActive,
    IReadOnlyList<AdminCancellationPolicyRuleDto> Rules,
    DateTimeOffset CreatedAt);

public record AdminCancellationPolicyRuleDto(
    Guid Id,
    int MinHoursBeforeStart,
    int? MaxHoursBeforeStart,
    decimal RefundPercent);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateCancellationPolicyRequest(
    string Name,
    DateTimeOffset? EffectiveFrom = null,
    IReadOnlyList<CreateCancellationPolicyRuleRequest> Rules = null!);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateCancellationPolicyRuleRequest(
    int MinHoursBeforeStart,
    int? MaxHoursBeforeStart,
    decimal RefundPercent);
