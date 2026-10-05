using CourtGo.Domain.Common;

namespace CourtGo.Domain.Entities;

public class Sport : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Court> Courts { get; set; } = new List<Court>();
}
