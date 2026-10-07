using System.Security.Cryptography;
using System.Text;
using CourtGo.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace CourtGo.Infrastructure.Auth;

/// <summary>
/// Refresh tokens are 64 random bytes (Base64Url). Because they have full entropy, a plain SHA-256
/// of the token is a sufficient storage hash; the plaintext is never persisted.
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly JwtSettings _settings;

    public RefreshTokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public IssuedRefreshToken Create(DateTimeOffset now)
    {
        var token = Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        return new IssuedRefreshToken(token, Hash(token), now.AddDays(_settings.RefreshTokenDays));
    }

    public string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
