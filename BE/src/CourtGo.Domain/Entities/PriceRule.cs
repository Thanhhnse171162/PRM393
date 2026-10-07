using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

public class PriceRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourtId { get; set; }
    public Court? Court { get; set; }

    public CourtGoDayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal PricePerHour { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<BookingSlot> BookingSlots { get; set; } = new List<BookingSlot>();
}
