namespace CourtGo.Application.SportCenters;

public record SportCenterSummaryDto(
    Guid Id,
    string Name,
    string AddressLine,
    string? Ward,
    string District,
    string City,
    decimal? Latitude,
    decimal? Longitude,
    string? PhoneNumber,
    string? CoverImageUrl,
    string Status,
    IReadOnlyList<string> Sports,
    decimal? PriceFrom,
    int TotalCourts
);
