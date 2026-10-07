namespace CourtGo.Domain.Entities;

public class UserPreference
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string? PreferredAreaName { get; set; }
    public decimal? PreferredLatitude { get; set; }
    public decimal? PreferredLongitude { get; set; }
    public bool BookingNotifications { get; set; } = true;
    public bool PaymentNotifications { get; set; } = true;
    public bool ReminderNotifications { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
