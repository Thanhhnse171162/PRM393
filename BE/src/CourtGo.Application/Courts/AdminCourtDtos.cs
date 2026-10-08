using System.Text.Json.Serialization;
using CourtGo.Domain.Enums;

namespace CourtGo.Application.Courts;

public record AdminCourtQuery(
    Guid? CenterId = null,
    Guid? SportId = null,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    CourtStatus? Status = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20);

public record AdminCourtListItemDto(
    Guid CourtId,
    Guid SportCenterId,
    string SportCenterName,
    Guid SportId,
    string SportName,
    string Code,
    string Name,
    string? SurfaceType,
    decimal BasePricePerHour,
    string Status,
    string? CoverImageUrl,
    DateTimeOffset CreatedAt);

public record AdminCourtDetailDto(
    Guid CourtId,
    Guid SportCenterId,
    string SportCenterName,
    Guid SportId,
    string SportName,
    string Code,
    string Name,
    string? SurfaceType,
    string? Description,
    string? CoverImageUrl,
    decimal BasePricePerHour,
    string Status,
    int ActiveBookingCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateCourtRequest(
    Guid SportCenterId,
    Guid SportId,
    string Code,
    string Name,
    string? SurfaceType = null,
    string? Description = null,
    string? CoverImageUrl = null,
    decimal BasePricePerHour = 0);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateCourtRequest(
    string Name,
    string? SurfaceType = null,
    string? Description = null,
    string? CoverImageUrl = null,
    decimal BasePricePerHour = 0,
    Guid? SportId = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateCourtStatusRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    CourtStatus Status);
