using CourtGo.Domain.Common;

namespace CourtGo.Domain.Entities;

/// <summary>One fixed 1-hour slot belonging to a <see cref="Booking"/>.</summary>
public class BookingSlot : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public Guid CourtId { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public decimal Price { get; set; }
}
