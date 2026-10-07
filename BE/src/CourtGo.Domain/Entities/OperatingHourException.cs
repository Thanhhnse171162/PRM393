namespace CourtGo.Domain.Entities;

public class OperatingHourException
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SportCenterId { get; set; }
    public SportCenter? SportCenter { get; set; }

    public DateOnly Date { get; set; }
    public bool IsClosed { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public string? Reason { get; set; }
}
