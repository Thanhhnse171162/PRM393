namespace CourtGo.Domain.Enums;

public enum BookingPaymentStatus : byte
{
    Unpaid = 1,
    DepositPaid = 2,
    FullyPaid = 3,
    RefundPending = 4,
    PartiallyRefunded = 5,
    Refunded = 6
}
