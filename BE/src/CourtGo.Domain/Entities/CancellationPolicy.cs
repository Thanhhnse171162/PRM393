namespace CourtGo.Domain.Entities;

public class CancellationPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CancellationPolicyRule> Rules { get; set; } = new List<CancellationPolicyRule>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
