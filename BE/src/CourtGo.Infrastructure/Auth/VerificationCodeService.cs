using System.Security.Cryptography;
using System.Text;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CourtGo.Infrastructure.Auth;

/// <summary>
/// OTP foundation (phone verification / forgot password). Stores an HMAC of the code, limits attempts,
/// and consumes codes on success. It does NOT send SMS and is not exposed through any endpoint yet.
/// </summary>
public class VerificationCodeService : IVerificationCodeService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly CourtGoDbContext _db;
    private readonly JwtSettings _settings;
    private readonly TimeProvider _time;

    public VerificationCodeService(CourtGoDbContext db, IOptions<JwtSettings> settings, TimeProvider time)
    {
        _db = db;
        _settings = settings.Value;
        _time = time;
    }

    public async Task<VerificationCodeIssue> IssueAsync(Guid? userId, string target, VerificationPurpose purpose, CancellationToken ct = default)
    {
        var now = _time.GetUtcNow();

        // Invalidate earlier unconsumed codes for the same target/purpose.
        var previous = await _db.VerificationCodes
            .Where(v => v.Target == target && v.Purpose == purpose && v.ConsumedAt == null)
            .ToListAsync(ct);
        foreach (var p in previous) p.ConsumedAt = now;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var expires = now.Add(Lifetime);

        _db.VerificationCodes.Add(new VerificationCode
        {
            UserId = userId,
            Target = target,
            Purpose = purpose,
            CodeHash = Hash(target, purpose, code),
            ExpiresAt = expires,
            CreatedAt = now
        });
        await _db.SaveChangesAsync(ct);

        return new VerificationCodeIssue(code, expires);
    }

    public async Task<bool> VerifyAsync(string target, VerificationPurpose purpose, string code, CancellationToken ct = default)
    {
        var now = _time.GetUtcNow();
        var entry = await _db.VerificationCodes
            .Where(v => v.Target == target && v.Purpose == purpose && v.ConsumedAt == null && v.ExpiresAt > now)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (entry is null || entry.AttemptCount >= MaxAttempts) return false;

        entry.AttemptCount++;
        var ok = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(entry.CodeHash), Encoding.UTF8.GetBytes(Hash(target, purpose, code)));
        if (ok) entry.ConsumedAt = now;

        await _db.SaveChangesAsync(ct);
        return ok;
    }

    private string Hash(string target, VerificationPurpose purpose, string code)
    {
        if (string.IsNullOrWhiteSpace(_settings.Key))
            throw new InvalidOperationException("Jwt:Key is required to hash verification codes.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.Key));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{target}|{(byte)purpose}|{code}")));
    }
}
