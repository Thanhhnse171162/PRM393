using CourtGo.Application.Courts;

namespace CourtGo.Application.SportCenters;

public record SportCenterDetailDto(
    Guid Id,
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
    IReadOnlyList<string> Sports,
    decimal? PriceFrom,
    int TotalCourts,
    IReadOnlyList<OperatingHourDto> OperatingHours,
    IReadOnlyList<CourtDto> Courts
);
