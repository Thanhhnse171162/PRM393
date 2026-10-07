using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;

namespace CourtGo.Application.Interfaces;

public interface IRefreshTokenRepository
{
    /// <summary>Returns the (tracked) token row for a token HASH, or null.</summary>
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default);

    void Add(RefreshToken token);

    /// <summary>Revokes every active refresh token of a user (used on refresh-token reuse).</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

public record IssuedRefreshToken(string Token, string Hash, DateTimeOffset ExpiresAt);

/// <summary>Generates opaque random refresh tokens and hashes them for storage.</summary>
public interface IRefreshTokenService
{
    IssuedRefreshToken Create(DateTimeOffset now);
    string Hash(string token);
}

public record VerificationCodeIssue(string Code, DateTimeOffset ExpiresAt);

/// <summary>
/// Foundation for phone verification / forgot password. Stores only a keyed hash of the code in
/// VerificationCodes. It does NOT deliver SMS: a future ISmsSender integration must send the code.
/// </summary>
public interface IVerificationCodeService
{
    Task<VerificationCodeIssue> IssueAsync(Guid? userId, string target, VerificationPurpose purpose, CancellationToken ct = default);
    Task<bool> VerifyAsync(string target, VerificationPurpose purpose, string code, CancellationToken ct = default);
}
