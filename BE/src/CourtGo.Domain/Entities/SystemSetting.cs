namespace CourtGo.Domain.Entities;

public class SystemSetting
{
    public byte Id { get; set; } = 1;
    public int HoldDurationMinutes { get; set; } = 10;
    public int MinBookingLeadMinutes { get; set; } = 60;
    public decimal DefaultDepositPercent { get; set; } = 30.00m;
    public bool AllowOutstandingCheckIn { get; set; } = false;
    public Guid? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
