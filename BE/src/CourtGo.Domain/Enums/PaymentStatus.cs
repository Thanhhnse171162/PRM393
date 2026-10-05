namespace CourtGo.Domain.Enums;

/// <summary>Money state of a booking. Never mixed into <see cref="BookingStatus"/>.</summary>
public enum PaymentStatus
{
    Unpaid = 0,
    DepositPaid = 1,
    FullyPaid = 2,
    RefundPending = 3,
    Refunded = 4,
    Failed = 5
}
