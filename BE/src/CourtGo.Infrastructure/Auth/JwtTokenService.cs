using System.Security.Claims;
using System.Text;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CourtGo.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    /// <summary>Short claim name used for the role (paired with RoleClaimType in JWT validation).</summary>
    public const string RoleClaimType = "role";

    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public string CreateAccessToken(Guid userId, string email, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(_settings.Key) || _settings.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters. Configure it via user-secrets or environment variables.");

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(RoleClaimType, role.ToString())
            }),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
