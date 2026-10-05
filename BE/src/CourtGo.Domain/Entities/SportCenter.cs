using CourtGo.Domain.Common;

namespace CourtGo.Domain.Entities;

/// <summary>A CourtGo branch (e.g. "CourtGo Sports Arena - Quan 7").</summary>
public class SportCenter : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? District { get; set; }
    public string? City { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ImageUrl { get; set; }
    public TimeOnly OpenTime { get; set; } = new(6, 0);
    public TimeOnly CloseTime { get; set; } = new(22, 0);
    public bool IsActive { get; set; } = true;

    public ICollection<Court> Courts { get; set; } = new List<Court>();
    public ICollection<StaffAssignment> StaffAssignments { get; set; } = new List<StaffAssignment>();
}
