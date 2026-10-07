namespace CourtGo.Domain.Enums;

public enum CancellationRequestStatus : byte
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    RefundPending = 4,
    Completed = 5
}
