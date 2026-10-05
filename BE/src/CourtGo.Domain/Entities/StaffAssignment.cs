namespace CourtGo.Domain.Entities;

/// <summary>Assigns a Staff user to ONE sport center (branch). Staff may operate every sport there.</summary>
public class StaffAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid SportCenterId { get; set; }
    public SportCenter? SportCenter { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
