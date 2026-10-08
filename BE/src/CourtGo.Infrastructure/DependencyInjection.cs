using CourtGo.Application.Interfaces;
using CourtGo.Infrastructure.Auth;
using CourtGo.Infrastructure.Data;
using CourtGo.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CourtGo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Connection string comes from configuration (ConnectionStrings:CourtGoDb with DefaultConnection fallback)
        var connectionString = configuration.GetConnectionString("CourtGoDb")
            ?? configuration.GetConnectionString("DefaultConnection");

        if (!services.Any(s => s.ServiceType == typeof(CourtGoDbContext)))
        {
            services.AddDbContext<CourtGoDbContext>(options =>
            {
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    options.UseSqlServer(connectionString);
                }
            });
        }

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IVerificationCodeService, VerificationCodeService>();
        services.AddScoped<ISportRepository, SportRepository>();
        services.AddScoped<ISportCenterRepository, SportCenterRepository>();
        services.AddScoped<ICourtRepository, CourtRepository>();
        services.AddScoped<IExpiredBookingHoldService, CourtGo.Infrastructure.Services.ExpiredBookingHoldService>();
        services.AddSingleton<IBookingCodeGenerator, CourtGo.Infrastructure.Services.BookingCodeGenerator>();
        services.AddScoped<IBookingHoldService, CourtGo.Infrastructure.Services.BookingHoldService>();
        services.AddScoped<IWalkInBookingService, CourtGo.Infrastructure.Services.BookingHoldService>();

        services.AddScoped<IBookingQueryService, CourtGo.Infrastructure.Services.BookingQueryService>();
        services.AddScoped<CourtGo.Infrastructure.Services.BookingCommandExecutor>();
        services.AddScoped<IStaffBookingService, CourtGo.Infrastructure.Services.StaffBookingService>();
        services.AddScoped<IStaffOperationsService, CourtGo.Infrastructure.Services.StaffOperationsService>();
        services.AddScoped<IStaffCourtService, CourtGo.Infrastructure.Services.StaffCourtService>();
        services.AddScoped<IBookingLifecycleService, CourtGo.Infrastructure.Services.BookingLifecycleService>();
        services.AddScoped<ICancellationService, CourtGo.Infrastructure.Services.CancellationService>();

        services.AddSingleton<IQrTokenGenerator, CourtGo.Infrastructure.Services.QrTokenGenerator>();
        services.AddScoped<IDepositPaymentService, CourtGo.Infrastructure.Services.DepositPaymentService>();
        services.AddScoped<INotificationService, CourtGo.Infrastructure.Services.NotificationService>();
        services.AddScoped<IReviewService, CourtGo.Infrastructure.Services.ReviewService>();
        services.AddScoped<IAdminStaffService, CourtGo.Infrastructure.Services.AdminStaffService>();
        services.AddScoped<IAdminSportCenterService, CourtGo.Infrastructure.Services.AdminSportCenterService>();
        services.AddScoped<IAdminSportService, CourtGo.Infrastructure.Services.AdminSportService>();
        services.AddScoped<IAdminCourtService, CourtGo.Infrastructure.Services.AdminCourtService>();
        services.AddScoped<IAdminOperatingHourService, CourtGo.Infrastructure.Services.AdminOperatingHourService>();
        services.AddScoped<IAdminPriceRuleService, CourtGo.Infrastructure.Services.AdminPriceRuleService>();
        services.AddScoped<IAdminSystemSettingService, CourtGo.Infrastructure.Services.AdminSystemSettingService>();
        services.AddScoped<IAdminCancellationPolicyService, CourtGo.Infrastructure.Services.AdminCancellationPolicyService>();
        services.AddScoped<IAdminBookingService, CourtGo.Infrastructure.Services.AdminBookingService>();
        services.AddScoped<IAdminDashboardService, CourtGo.Infrastructure.Services.AdminDashboardService>();
        services.AddScoped<IAdminReportService, CourtGo.Infrastructure.Services.AdminReportService>();

        return services;
    }
}
