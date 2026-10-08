using System.Text.Json.Serialization;
using CourtGo.Domain.Enums;

namespace CourtGo.Application.SportCenters;

public record AdminSportCenterQuery(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    SportCenterStatus? Status = null,
    string? City = null,
    string? District = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20);

public record AdminSportCenterListItemDto(
    Guid CenterId,
    string Name,
    string Address,
    string City,
    string District,
    string? Phone,
    decimal? Latitude,
    decimal? Longitude,
    string TimeZoneId,
    string Status,
    int CourtCount,
    int ActiveCourtCount,
    DateTimeOffset CreatedAt);

public record AdminSportCenterDetailDto(
    Guid CenterId,
    string Name,
    string AddressLine,
    string? Ward,
    string District,
    string City,
    decimal? Latitude,
    decimal? Longitude,
    string TimeZoneId,
    string? PhoneNumber,
    string? CoverImageUrl,
    string Status,
    int TotalCourts,
    int ActiveCourts,
    int AssignedActiveStaffCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateSportCenterRequest(
    string Name,
    string AddressLine,
    string? Ward,
    string District,
    string City,
    decimal? Latitude = null,
    decimal? Longitude = null,
    string? TimeZoneId = null,
    string? PhoneNumber = null,
    string? CoverImageUrl = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateSportCenterRequest(
    string Name,
    string AddressLine,
    string? Ward,
    string District,
    string City,
    decimal? Latitude = null,
    decimal? Longitude = null,
    string? TimeZoneId = null,
    string? PhoneNumber = null,
    string? CoverImageUrl = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateSportCenterStatusRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    SportCenterStatus Status);
