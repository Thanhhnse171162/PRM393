using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly CourtGoDbContext _db;

    public UserRepository(CourtGoDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> FindByPhoneAsync(string phoneNumber, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);
}
