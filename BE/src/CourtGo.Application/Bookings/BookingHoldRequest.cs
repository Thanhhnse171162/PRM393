namespace CourtGo.Application.Bookings;

public record BookingHoldRequest(
    Guid CourtId,
    List<DateTimeOffset>? SlotStartAts
);
