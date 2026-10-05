using CourtGo.Domain.Common;
using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

/// <summary>A single money movement (deposit, remaining payment or refund) for a booking.</summary>
public class Payment : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public decimal Amount { get; set; }
    public PaymentType Type { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;

    public string? TransactionReference { get; set; }
    public DateTime? PaidAt { get; set; }

    /// <summary>Staff who collected the remaining amount at the branch (if any).</summary>
    public Guid? CollectedByUserId { get; set; }
}
