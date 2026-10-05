using CourtGo.Application.Interfaces;
using CourtGo.Infrastructure.Auth;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CourtGo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // The connection string is NEVER hardcoded; it comes from configuration
        // (appsettings.Development.json, user-secrets or environment variables).
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<CourtGoDbContext>(options =>
            options.UseSqlServer(connectionString ?? string.Empty));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
