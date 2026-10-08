namespace CourtGo.Application.Bookings;

public record BookingHoldResponse(
    Guid BookingId,
    string BookingCode,
    string BookingStatus,
    string PaymentStatus,
    BookingHoldCourtDto Court,
    BookingHoldCenterDto Center,
    BookingHoldSportDto Sport,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int DurationMinutes,
    IReadOnlyList<BookingHoldSlotDto> Slots,
    decimal TotalAmount,
    decimal DepositPercent,
    decimal DepositAmount,
    decimal RemainingAmount,
    DateTimeOffset HoldExpiresAt
);

public record BookingHoldCourtDto(
    Guid Id,
    string Code,
    string Name
);

public record BookingHoldCenterDto(
    Guid Id,
    string Name
);

public record BookingHoldSportDto(
    Guid Id,
    string Name
);

public record BookingHoldSlotDto(
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    decimal Price
);
