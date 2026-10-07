namespace CourtGo.Domain.Entities;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public Guid CustomerUserId { get; set; }
    public User? CustomerUser { get; set; }

    public byte Rating { get; set; } = 5;
    public string? Comment { get; set; }
    public bool IsVisible { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
