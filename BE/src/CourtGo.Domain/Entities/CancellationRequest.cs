using CourtGo.Domain.Common;
using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

public class CancellationRequest : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public Guid RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }

    public CancellationRequestSource RequestSource { get; set; } = CancellationRequestSource.Customer;
    public string Reason { get; set; } = string.Empty;
    public CancellationRequestStatus Status { get; set; } = CancellationRequestStatus.Pending;

    public decimal CalculatedRefundAmount { get; set; }
    public decimal? ApprovedRefundAmount { get; set; }

    public Guid? ProcessedByUserId { get; set; }
    public User? ProcessedByUser { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }
}
