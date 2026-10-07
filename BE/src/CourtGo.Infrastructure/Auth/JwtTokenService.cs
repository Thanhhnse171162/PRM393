using System.Security.Claims;
using System.Text;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
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

    public AccessTokenResult CreateAccessToken(User user, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(_settings.Key) || _settings.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters. Configure it via user-secrets or environment variables.");

        var expires = now.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("name", user.FullName),
            new(RoleClaimType, user.Role.ToString())
        };
        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return new AccessTokenResult(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
