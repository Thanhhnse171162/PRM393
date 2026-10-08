using System.Text.Json.Serialization;

namespace CourtGo.Application.Sports;

public record AdminSportQuery(
    bool? IsActive = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20);

public record AdminSportDto(
    Guid Id,
    string Code,
    string Name,
    string? IconUrl,
    int DisplayOrder,
    bool IsActive,
    int CourtCount,
    DateTimeOffset CreatedAt);

public record AdminSportDetailDto(
    Guid Id,
    string Code,
    string Name,
    string? IconUrl,
    int DisplayOrder,
    bool IsActive,
    int CourtCount,
    DateTimeOffset CreatedAt);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateSportRequest(
    string? Code,
    string Name,
    string? IconUrl = null,
    int DisplayOrder = 0);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateSportRequest(
    string Name,
    string? IconUrl = null,
    int DisplayOrder = 0);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateSportStatusRequest(
    bool IsActive);
