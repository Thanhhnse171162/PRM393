using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

public class OperatingHour
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SportCenterId { get; set; }
    public SportCenter? SportCenter { get; set; }

    public CourtGoDayOfWeek DayOfWeek { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public bool IsClosed { get; set; }
}
