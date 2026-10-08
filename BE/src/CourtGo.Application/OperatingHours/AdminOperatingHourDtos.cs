using System.Text.Json.Serialization;
using CourtGo.Domain.Enums;

namespace CourtGo.Application.OperatingHours;

public record AdminCenterOperatingHoursDto(
    Guid CenterId,
    string TimeZoneId,
    IReadOnlyList<AdminOperatingHourItemDto> Days);

public record AdminOperatingHourItemDto(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    CourtGoDayOfWeek DayOfWeek,
    bool IsClosed,
    TimeOnly? OpenTime,
    TimeOnly? CloseTime);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateOperatingHoursRequest(
    IReadOnlyList<UpdateOperatingHourItemRequest> Days);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateOperatingHourItemRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    CourtGoDayOfWeek DayOfWeek,
    bool IsClosed,
    TimeOnly? OpenTime = null,
    TimeOnly? CloseTime = null);

public record AdminOperatingHourExceptionDto(
    Guid ExceptionId,
    Guid SportCenterId,
    DateOnly Date,
    bool IsClosed,
    TimeOnly? OpenTime,
    TimeOnly? CloseTime,
    string? Reason);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateOperatingHourExceptionRequest(
    DateOnly Date,
    bool IsClosed,
    TimeOnly? OpenTime = null,
    TimeOnly? CloseTime = null,
    string? Reason = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateOperatingHourExceptionRequest(
    bool IsClosed,
    TimeOnly? OpenTime = null,
    TimeOnly? CloseTime = null,
    string? Reason = null);
