using CourtGo.Domain.Common;
using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

public class Booking : BaseEntity
{
    public string BookingCode { get; set; } = string.Empty;

    /// <summary>Null for walk-in bookings created by Staff without an account.</summary>
    public Guid? CustomerUserId { get; set; }
    public User? CustomerUser { get; set; }

    // Snapshot fields to preserve historical accuracy
    public string CustomerNameSnapshot { get; set; } = string.Empty;
    public string CustomerPhoneSnapshot { get; set; } = string.Empty;
    public string? CustomerEmailSnapshot { get; set; }

    public Guid CourtId { get; set; }
    public Court? Court { get; set; }

    public string CourtNameSnapshot { get; set; } = string.Empty;
    public string CenterNameSnapshot { get; set; } = string.Empty;
    public string SportNameSnapshot { get; set; } = string.Empty;

    public BookingSource Source { get; set; } = BookingSource.Online;
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public int DurationMinutes { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal DepositPercentSnapshot { get; set; }
    public decimal DepositAmount { get; set; }

    public BookingStatus BookingStatus { get; set; } = BookingStatus.PendingPayment;
    public BookingPaymentStatus PaymentStatus { get; set; } = BookingPaymentStatus.Unpaid;

    public DateTimeOffset? HoldExpiresAt { get; set; }
    public string? QrToken { get; set; }

    public Guid? CancellationPolicyId { get; set; }
    public CancellationPolicy? CancellationPolicy { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    // Navigation collections
    public ICollection<BookingSlot> Slots { get; set; } = new List<BookingSlot>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public CheckIn? CheckIn { get; set; }
    public ICollection<BookingStatusHistory> StatusHistories { get; set; } = new List<BookingStatusHistory>();
    public ICollection<CancellationRequest> CancellationRequests { get; set; } = new List<CancellationRequest>();
    public Review? Review { get; set; }
}
