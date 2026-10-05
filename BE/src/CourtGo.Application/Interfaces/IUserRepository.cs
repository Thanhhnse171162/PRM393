using CourtGo.Domain.Entities;

namespace CourtGo.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> FindByPhoneAsync(string phoneNumber, CancellationToken ct = default);
}
