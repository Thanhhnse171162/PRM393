using System.Text.Json.Serialization;
using CourtGo.Domain.Enums;

namespace CourtGo.Application.PriceRules;

public record AdminPriceRuleDto(
    Guid Id,
    Guid CourtId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    CourtGoDayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal PricePerHour,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    DateTimeOffset CreatedAt);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreatePriceRuleRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    CourtGoDayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal PricePerHour,
    DateOnly? EffectiveFrom = null,
    DateOnly? EffectiveTo = null,
    bool IsActive = true);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdatePriceRuleRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    CourtGoDayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal PricePerHour,
    DateOnly? EffectiveFrom = null,
    DateOnly? EffectiveTo = null,
    bool IsActive = true);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdatePriceRuleStatusRequest(
    bool IsActive);
