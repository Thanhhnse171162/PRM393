namespace CourtGo.Infrastructure.Auth;

/// <summary>Bound from the "Jwt" configuration section. The Key must come from user-secrets / env vars.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CourtGo";
    public string Audience { get; set; } = "CourtGo.Mobile";

    /// <summary>HMAC signing secret (>= 32 chars). Never commit it.</summary>
    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 30;
}
