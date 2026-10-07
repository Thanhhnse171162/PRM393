namespace CourtGo.Domain.Enums;

public enum NotificationType : byte
{
    Booking = 1,
    Payment = 2,
    Reminder = 3,
    Cancellation = 4,
    Refund = 5,
    Court = 6,
    System = 7
}
