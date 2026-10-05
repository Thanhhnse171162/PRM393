using CourtGo.Domain.Common;
using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

/// <summary>
/// ONE booking per court per contiguous time range, made of 1..n consecutive
/// 1-hour <see cref="BookingSlot"/>s. BookingStatus and PaymentStatus are separate.
/// </summary>
public class Booking : BaseEntity
{
    /// <summary>Null for walk-in bookings created by Staff without an account.</summary>
    public Guid? CustomerId { get; set; }
    public User? Customer { get; set; }

    public Guid CourtId { get; set; }
    public Court? Court { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    /// <summary>Temporary hold expiry while the booking is PendingPayment.</summary>
    public DateTime? HoldExpiresAt { get; set; }

    /// <summary>Opaque code encoded into the booking QR for Staff check-in.</summary>
    public string? CheckInCode { get; set; }

    public bool IsWalkIn { get; set; }
    public string? Note { get; set; }

    public ICollection<BookingSlot> Slots { get; set; } = new List<BookingSlot>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
