using CourtGo.Domain.Common;
using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

public class Court : BaseEntity
{
    public Guid SportCenterId { get; set; }
    public SportCenter? SportCenter { get; set; }

    public Guid SportId { get; set; }
    public Sport? Sport { get; set; }

    public string Name { get; set; } = string.Empty;
    public CourtStatus Status { get; set; } = CourtStatus.Active;

    /// <summary>Price for one fixed 1-hour slot (VND).</summary>
    public decimal PricePerHour { get; set; }
}
