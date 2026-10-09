using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

/// <summary>
/// A single transaction/history entry (Deposit, Remaining, or Refund) for a booking.
/// Never design Payment as a single mutable PaidAmount.
/// </summary>
public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public PaymentKind PaymentKind { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public PaymentTransactionStatus TransactionStatus { get; set; } = PaymentTransactionStatus.Pending;

    public string? ProviderTransactionId { get; set; }
    /// <summary>Provider-qualified receipt, distinct from our merchant order reference.</summary>
    public string? ProviderReceiptId { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? GatewayName { get; set; }

    /// <summary>Points to original payment if this row is a refund.</summary>
    public Guid? RelatedPaymentId { get; set; }
    public Payment? RelatedPayment { get; set; }
    public ICollection<Payment> RefundPayments { get; set; } = new List<Payment>();

    public Guid? PaidByUserId { get; set; }
    public User? PaidByUser { get; set; }

    public Guid? ConfirmedByUserId { get; set; }
    public User? ConfirmedByUser { get; set; }

    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
