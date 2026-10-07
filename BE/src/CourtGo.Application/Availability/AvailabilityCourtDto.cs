namespace CourtGo.Application.Availability;

public record AvailabilityCourtDto(
    Guid Id,
    string Code,
    string Name,
    Guid SportId,
    string SportName,
    Guid CenterId,
    string CenterName
);
