using System.Text.Json.Serialization;

namespace CourtGo.Application.Settings;

public record SystemSettingsDto(
    int HoldDurationMinutes,
    int MinBookingLeadMinutes,
    decimal DefaultDepositPercent,
    bool AllowOutstandingCheckIn,
    DateTimeOffset UpdatedAt,
    Guid? UpdatedByUserId);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateSystemSettingsRequest(
    int HoldDurationMinutes,
    int MinBookingLeadMinutes,
    decimal DefaultDepositPercent,
    bool AllowOutstandingCheckIn);
