using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

public class UserDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string DeviceToken { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
    public string? DeviceName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
