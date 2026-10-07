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

        return services;
    }
}
