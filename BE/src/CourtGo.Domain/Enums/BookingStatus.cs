namespace CourtGo.Domain.Enums;

/// <summary>Lifecycle of a booking. Payment state is tracked separately in <see cref="PaymentStatus"/>.</summary>
public enum BookingStatus
{
    PendingPayment = 0,
    Confirmed = 1,
    CheckedIn = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    Expired = 6,
    NoShow = 7
}
