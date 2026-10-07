namespace CourtGo.Domain.Entities;

public class StaffAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StaffUserId { get; set; }
    public User? StaffUser { get; set; }

    public Guid SportCenterId { get; set; }
    public SportCenter? SportCenter { get; set; }

    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;
}
