using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;

namespace CourtGo.UnitTests;

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public IReadOnlyList<User> All => _users;
    public void Add(User user) => _users.Add(user);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> FindByPhoneAsync(string phoneNumber, CancellationToken ct = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.PhoneNumber == phoneNumber));

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken ct = default) =>
        Task.FromResult(_users.Any(u => u.PhoneNumber == phoneNumber));

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = new();

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default) =>
        Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

    public void Add(RefreshToken token) => Tokens.Add(token);

    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        foreach (var t in Tokens.Where(t => t.UserId == userId && t.RevokedAt is null)) t.RevokedAt = revokedAt;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class FakeTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => Now;
}
