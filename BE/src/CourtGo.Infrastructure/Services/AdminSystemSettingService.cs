using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Settings;
using CourtGo.Domain.Entities;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class AdminSystemSettingService(
    CourtGoDbContext db,
    TimeProvider clock) : IAdminSystemSettingService
{
    public async Task<SystemSettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        var settings = await db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1, ct);
        if (settings is null)
        {
            // If row 1 is absent, create default row
            var defaultSettings = new SystemSetting
            {
                Id = 1,
                HoldDurationMinutes = 10,
                MinBookingLeadMinutes = 60,
                DefaultDepositPercent = 30.00m,
                AllowOutstandingCheckIn = false,
                UpdatedAt = clock.GetUtcNow()
            };
            db.SystemSettings.Add(defaultSettings);
            await db.SaveChangesAsync(ct);
            return ToDto(defaultSettings);
        }

        return ToDto(settings);
    }

    public async Task<SystemSettingsDto> UpdateSettingsAsync(
        Guid updatedByUserId,
        UpdateSystemSettingsRequest request,
        CancellationToken ct = default)
    {
        if (request.HoldDurationMinutes <= 0)
            throw new ValidationException("HoldDurationMinutes must be greater than 0.");
        if (request.HoldDurationMinutes > 1440)
            throw new ValidationException("HoldDurationMinutes cannot exceed 1440 minutes (24 hours).");

        if (request.MinBookingLeadMinutes < 0)
            throw new ValidationException("MinBookingLeadMinutes cannot be negative.");
        if (request.MinBookingLeadMinutes > 10080)
            throw new ValidationException("MinBookingLeadMinutes cannot exceed 10080 minutes (7 days).");

        if (request.DefaultDepositPercent < 0 || request.DefaultDepositPercent > 100)
            throw new ValidationException("DefaultDepositPercent must be between 0 and 100.");

        var settings = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == 1, ct);
        if (settings is null)
        {
            settings = new SystemSetting
            {
                Id = 1,
                HoldDurationMinutes = request.HoldDurationMinutes,
                MinBookingLeadMinutes = request.MinBookingLeadMinutes,
                DefaultDepositPercent = request.DefaultDepositPercent,
                AllowOutstandingCheckIn = request.AllowOutstandingCheckIn,
                UpdatedByUserId = updatedByUserId,
                UpdatedAt = clock.GetUtcNow()
            };
            db.SystemSettings.Add(settings);
        }
        else
        {
            settings.HoldDurationMinutes = request.HoldDurationMinutes;
            settings.MinBookingLeadMinutes = request.MinBookingLeadMinutes;
            settings.DefaultDepositPercent = request.DefaultDepositPercent;
            settings.AllowOutstandingCheckIn = request.AllowOutstandingCheckIn;
            settings.UpdatedByUserId = updatedByUserId;
            settings.UpdatedAt = clock.GetUtcNow();
        }

        await db.SaveChangesAsync(ct);

        return ToDto(settings);
    }

    private static SystemSettingsDto ToDto(SystemSetting s) => new(
        s.HoldDurationMinutes,
        s.MinBookingLeadMinutes,
        s.DefaultDepositPercent,
        s.AllowOutstandingCheckIn,
        s.UpdatedAt,
        s.UpdatedByUserId);
}
