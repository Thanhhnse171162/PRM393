using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Data;

/// <summary>
/// DEVELOPMENT ONLY demo accounts. Runs only in the Development environment with Seed:Enabled=true.
/// The password comes from configuration "Seed:DemoPassword" (user-secrets recommended); the fallback
/// below is a public, development-only value and must never be used outside local development.
/// Only inserts missing rows; never touches existing data and never creates a SportCenter.
/// </summary>
public static class DbSeeder
{
    public const string DefaultDemoPassword = "Demo@123456";

    public static async Task SeedAsync(CourtGoDbContext db, IPasswordHasher hasher, string? password = null, CancellationToken ct = default)
    {
        var pw = string.IsNullOrWhiteSpace(password) ? DefaultDemoPassword : password;

        await EnsureUserAsync(db, hasher, pw, "Demo Customer", "customer@courtgo.vn", "0900000001", UserRole.Customer, ct);
        var staff = await EnsureUserAsync(db, hasher, pw, "Demo Staff", "staff@courtgo.vn", "0900000002", UserRole.Staff, ct);
        await EnsureUserAsync(db, hasher, pw, "Demo Admin", "admin@courtgo.vn", "0900000003", UserRole.Admin, ct);

        // Only assign the demo staff when a sport center already exists (no invented FK data).
        var center = await db.SportCenters.OrderBy(c => c.CreatedAt).FirstOrDefaultAsync(ct);
        if (center is not null && !await db.StaffAssignments.AnyAsync(a => a.StaffUserId == staff.Id && a.IsActive, ct))
        {
            db.StaffAssignments.Add(new StaffAssignment { StaffUserId = staff.Id, SportCenterId = center.Id });
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task<User> EnsureUserAsync(
        CourtGoDbContext db, IPasswordHasher hasher, string password, string name, string email, string phone, UserRole role, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email || u.PhoneNumber == phone, ct);
        if (user is not null) return user;

        user = new User
        {
            FullName = name,
            Email = email,
            PhoneNumber = phone,
            PasswordHash = hasher.Hash(password),
            Role = role,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
