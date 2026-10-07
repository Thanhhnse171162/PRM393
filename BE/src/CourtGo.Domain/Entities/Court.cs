using CourtGo.Domain.Common;
using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

public class Court : BaseEntity
{
    public Guid SportCenterId { get; set; }
    public SportCenter? SportCenter { get; set; }

    public Guid SportId { get; set; }
    public Sport? Sport { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? SurfaceType { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal BasePricePerHour { get; set; }
    public CourtStatus Status { get; set; } = CourtStatus.Active;

    public ICollection<PriceRule> PriceRules { get; set; } = new List<PriceRule>();
    public ICollection<CourtBlock> CourtBlocks { get; set; } = new List<CourtBlock>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<BookingSlot> BookingSlots { get; set; } = new List<BookingSlot>();
}
