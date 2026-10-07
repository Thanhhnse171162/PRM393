namespace CourtGo.Application.Courts;

public record CourtDto(
    Guid Id,
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
    string Status
);
