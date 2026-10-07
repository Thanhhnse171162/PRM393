using CourtGo.Domain.Common;
using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? PhoneVerifiedAt { get; set; }
    public DateTimeOffset? EmailVerifiedAt { get; set; }

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public UserPreference? UserPreference { get; set; }
    public ICollection<UserDevice> UserDevices { get; set; } = new List<UserDevice>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<StaffAssignment> StaffAssignments { get; set; } = new List<StaffAssignment>();
    public ICollection<Booking> CustomerBookings { get; set; } = new List<Booking>();
    public ICollection<Booking> CreatedBookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
