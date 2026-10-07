using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Infrastructure.Data;
using Microsoft.Data.SqlClient;
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

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.Email == email, ct);

    public Task<bool> ExistsByPhoneAsync(string phoneNumber, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.PhoneNumber == phoneNumber, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 } sql)
        {
            // Race between the exists-check and the insert: the unique indexes are the final guard.
            _db.Entry(user).State = EntityState.Detached;
            throw sql.Message.Contains("UX_Users_Email", StringComparison.OrdinalIgnoreCase)
                ? new ConflictException("Email is already registered.", ErrorCodes.EmailAlreadyExists)
                : new ConflictException("Phone number is already registered.", ErrorCodes.PhoneAlreadyExists);
        }
    }
}
