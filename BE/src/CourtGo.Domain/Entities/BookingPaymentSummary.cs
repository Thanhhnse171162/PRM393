namespace CourtGo.Domain.Entities;

/// <summary>
/// Read-only keyless model mapped to SQL view <c>vw_BookingPaymentSummary</c>.
/// </summary>
public class BookingPaymentSummary
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal SuccessfulChargeAmount { get; set; }
    public decimal SuccessfulRefundAmount { get; set; }
    public decimal RemainingToCollect { get; set; }
}
