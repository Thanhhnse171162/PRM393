using CourtGo.Domain.Common;
using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

public class SportCenter : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? Ward { get; set; }
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public string? PhoneNumber { get; set; }
    public string? CoverImageUrl { get; set; }
    public SportCenterStatus Status { get; set; } = SportCenterStatus.Active;

    public ICollection<StaffAssignment> StaffAssignments { get; set; } = new List<StaffAssignment>();
    public ICollection<Court> Courts { get; set; } = new List<Court>();
    public ICollection<OperatingHour> OperatingHours { get; set; } = new List<OperatingHour>();
    public ICollection<OperatingHourException> OperatingHourExceptions { get; set; } = new List<OperatingHourException>();
}
