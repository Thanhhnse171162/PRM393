namespace CourtGo.Application.Availability;

public record AvailabilitySlotDto(
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    decimal Price,
    AvailabilitySlotStatus Status
);
