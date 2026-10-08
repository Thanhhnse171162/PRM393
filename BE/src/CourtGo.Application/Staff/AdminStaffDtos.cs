using System.Text.Json.Serialization;
using CourtGo.Application.Common;

namespace CourtGo.Application.Staff;

public record AdminStaffQuery(
    Guid? CenterId = null,
    bool? IsActive = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20);

public record AssignedCenterDto(
    Guid CenterId,
    string CenterName);

public record AdminStaffSummaryDto(
    Guid StaffId,
    string FullName,
    string? Email,
    string PhoneNumber,
    bool IsActive,
    AssignedCenterDto? AssignedCenter,
    DateTimeOffset CreatedAt);

public record StaffAssignmentHistoryDto(
    Guid AssignmentId,
    Guid CenterId,
    string CenterName,
    DateTimeOffset AssignedAt,
    bool IsActive);

public record AdminStaffDetailDto(
    Guid StaffId,
    string FullName,
    string? Email,
    string PhoneNumber,
    bool IsActive,
    AssignedCenterDto? AssignedCenter,
    IReadOnlyList<StaffAssignmentHistoryDto> AssignmentHistory,
    DateTimeOffset CreatedAt);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateStaffRequest(
    string FullName,
    string? Email,
    string PhoneNumber,
    string Password,
    Guid SportCenterId);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateStaffProfileRequest(
    string FullName,
    string? Email,
    string PhoneNumber);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record UpdateStaffStatusRequest(
    bool IsActive);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record ReassignStaffRequest(
    Guid SportCenterId);
