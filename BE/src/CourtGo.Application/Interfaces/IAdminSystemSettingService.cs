using CourtGo.Application.Settings;

namespace CourtGo.Application.Interfaces;

public interface IAdminSystemSettingService
{
    Task<SystemSettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task<SystemSettingsDto> UpdateSettingsAsync(Guid updatedByUserId, UpdateSystemSettingsRequest request, CancellationToken ct = default);
}
