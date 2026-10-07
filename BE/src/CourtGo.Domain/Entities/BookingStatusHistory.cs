using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

public class BookingStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public BookingStatus? FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }

    public Guid? ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }

    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
