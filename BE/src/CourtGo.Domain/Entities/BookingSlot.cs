using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

/// <summary>One fixed 1-hour slot belonging to a <see cref="Booking"/>.</summary>
public class BookingSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public Guid CourtId { get; set; }
    public Court? Court { get; set; }

    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }

    public decimal UnitPrice { get; set; }

    public Guid? PriceRuleId { get; set; }
    public PriceRule? PriceRule { get; set; }

    public ReservationState ReservationState { get; set; } = ReservationState.Held;
    public bool IsOccupying { get; set; } = true;
    public DateTimeOffset? HoldExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
