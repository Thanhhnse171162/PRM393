using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Data;

/// <summary>
/// DEVELOPMENT ONLY demo data. Enabled with configuration Seed:Enabled=true.
/// Demo password is public and must never be used outside local development.
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "Demo@123456";

    public static async Task SeedAsync(CourtGoDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        await EnsureUserAsync(db, hasher, "Demo Customer", "customer@courtgo.local", "0900000001", UserRole.Customer, ct);
        var staff = await EnsureUserAsync(db, hasher, "Demo Staff", "staff@courtgo.local", "0900000002", UserRole.Staff, ct);
        await EnsureUserAsync(db, hasher, "Demo Admin", "admin@courtgo.local", "0900000003", UserRole.Admin, ct);

        var center = await db.SportCenters.FirstOrDefaultAsync(ct);
        if (center is null)
        {
            center = new SportCenter
            {
                Name = "CourtGo Sports Arena - Quan 7",
                Address = "Quan 7, TP.HCM",
                District = "Quan 7",
                City = "TP.HCM"
            };
            db.SportCenters.Add(center);
        }

        if (!await db.StaffAssignments.AnyAsync(a => a.UserId == staff.Id, ct))
            db.StaffAssignments.Add(new StaffAssignment { UserId = staff.Id, SportCenterId = center.Id });

        await db.SaveChangesAsync(ct);
    }

    private static async Task<User> EnsureUserAsync(
        CourtGoDbContext db, IPasswordHasher hasher, string name, string email, string phone, UserRole role, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is not null) return user;

        user = new User
        {
            FullName = name,
            Email = email,
            PhoneNumber = phone,
            PasswordHash = hasher.Hash(DemoPassword),
            Role = role,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
