using CourtGo.Application.OperatingHours;

namespace CourtGo.Application.Interfaces;

public interface IAdminOperatingHourService
{
    Task<AdminCenterOperatingHoursDto> GetOperatingHoursAsync(Guid centerId, CancellationToken ct = default);
    Task<AdminCenterOperatingHoursDto> UpdateOperatingHoursAsync(Guid centerId, UpdateOperatingHoursRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AdminOperatingHourExceptionDto>> GetOperatingHourExceptionsAsync(Guid centerId, DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default);
    Task<AdminOperatingHourExceptionDto> CreateOperatingHourExceptionAsync(Guid centerId, CreateOperatingHourExceptionRequest request, CancellationToken ct = default);
    Task<AdminOperatingHourExceptionDto> UpdateOperatingHourExceptionAsync(Guid centerId, Guid exceptionId, UpdateOperatingHourExceptionRequest request, CancellationToken ct = default);
    Task DeleteOperatingHourExceptionAsync(Guid centerId, Guid exceptionId, CancellationToken ct = default);
}
