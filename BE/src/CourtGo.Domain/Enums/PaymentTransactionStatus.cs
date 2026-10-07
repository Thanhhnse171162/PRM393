namespace CourtGo.Domain.Enums;

public enum PaymentTransactionStatus : byte
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4
}
