namespace CourtGo.Domain.Entities;

public class CancellationPolicyRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CancellationPolicyId { get; set; }
    public CancellationPolicy? CancellationPolicy { get; set; }

    public int MinHoursBeforeStart { get; set; }
    public int? MaxHoursBeforeStart { get; set; }
    public decimal RefundPercent { get; set; }
}
