using System.Text;
using CourtGo.Api.Middleware;
using CourtGo.Application.Auth;
using CourtGo.Application.Interfaces;
using CourtGo.Infrastructure;
using CourtGo.Infrastructure.Auth;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Optional local overrides (git-ignored). Real secrets: user-secrets or environment variables.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        // Malformed JSON / model binding errors use the same error shape as business validation errors.
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState
                .Where(e => e.Value is { Errors.Count: > 0 })
                .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Invalid value." : x.ErrorMessage).ToArray());
            return new Microsoft.AspNetCore.Mvc.ObjectResult(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = 400,
                Title = "Validation failed",
                Detail = "One or more validation errors occurred.",
                Instance = ctx.HttpContext.Request.Path,
                Extensions = { ["code"] = "VALIDATION_ERROR", ["errors"] = errors }
            })
            { StatusCode = 400, ContentTypes = { "application/problem+json" } };
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CourtGo API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT access token only (without the 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Database connection string: ConnectionStrings:CourtGoDb (with DefaultConnection fallback)
var connectionString = builder.Configuration.GetConnectionString("CourtGoDb")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<CourtGoDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseSqlServer(connectionString);
    }
});

// Infrastructure: EF Core + SQL Server, JWT service, password hasher, repositories.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISportService, CourtGo.Application.Sports.SportService>();
builder.Services.AddScoped<ISportCenterService, CourtGo.Application.SportCenters.SportCenterService>();
builder.Services.AddScoped<ICourtService, CourtGo.Application.Courts.CourtService>();
builder.Services.AddScoped<IAvailabilityService, CourtGo.Infrastructure.Services.AvailabilityService>();

// JWT bearer authentication. Options are bound lazily from JwtSettings (configuration/user-secrets).
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<Microsoft.Extensions.Options.IOptions<JwtSettings>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        options.MapInboundClaims = false; // keep short claim names: sub, email, role
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            // Role claim is "role" -> [Authorize(Roles = "Customer|Staff|Admin")] works.
            RoleClaimType = JwtTokenService.RoleClaimType,
            // Falls back to a throw-away random key so the app can start without secrets;
            // tokens are then simply not valid until Jwt:Key is configured.
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(jwt.Key) ? Guid.NewGuid().ToString("N") : jwt.Key))
        };

        // Same error shape for 401/403 produced by the authentication/authorization middleware.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                return CourtGo.Api.Middleware.ApiProblem.WriteAsync(ctx.HttpContext, StatusCodes.Status401Unauthorized,
                    "UNAUTHORIZED", "Unauthorized", "Authentication is required or the access token is invalid/expired.");
            },
            OnForbidden = ctx => CourtGo.Api.Middleware.ApiProblem.WriteAsync(ctx.HttpContext, StatusCodes.Status403Forbidden,
                "FORBIDDEN", "Forbidden", "You do not have permission to perform this action.")
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomerOnly", p => p.RequireRole("Customer"));
    options.AddPolicy("StaffOnly", p => p.RequireRole("Staff"));
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

// CORS for development (mobile apps do not need CORS; this helps Swagger/web tools).
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddProblemDetails();

var app = builder.Build();

// Safe development-only database connectivity check
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var canConnect = await db.Database.CanConnectAsync();
            if (canConnect)
            {
                logger.LogInformation("Database connection check succeeded: connected to CourtGoDb.");
            }
            else
            {
                logger.LogWarning("Database connection check returned false for CourtGoDb.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning("Database connection check failed: {Message}", ex.Message);
    }
}

// Development-only demo accounts (Seed:Enabled=true). Never runs outside Development.
if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Seed:Enabled"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            app.Configuration["Seed:DemoPassword"]);
        logger.LogInformation("Demo accounts seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database seeding failed.");
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("Development");
}

// HTTPS redirection is skipped in Development so the Android emulator can use plain http://10.0.2.2.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Needed by WebApplicationFactory in integration tests.
public partial class Program { }
