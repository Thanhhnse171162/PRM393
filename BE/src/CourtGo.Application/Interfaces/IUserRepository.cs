using CourtGo.Domain.Entities;

namespace CourtGo.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> FindByPhoneAsync(string phoneNumber, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken ct = default);

    /// <summary>Persists a new user. Throws ConflictException if a unique index is violated (race).</summary>
    Task AddAsync(User user, CancellationToken ct = default);
}
