namespace CourtGo.Application.Availability;

public record CourtAvailabilityDto(
    AvailabilityCourtDto Court,
    string Date,
    bool IsClosed,
    OpeningHoursDto? OpeningHours,
    IReadOnlyList<AvailabilitySlotDto> Slots
);
