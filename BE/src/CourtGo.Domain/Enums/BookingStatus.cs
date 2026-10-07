namespace CourtGo.Domain.Enums;

public enum BookingStatus : byte
{
    PendingPayment = 1,
    Confirmed = 2,
    CheckedIn = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6,
    Expired = 7,
    NoShow = 8
}
