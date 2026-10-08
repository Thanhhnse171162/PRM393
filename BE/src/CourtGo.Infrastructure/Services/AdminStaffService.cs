using System.Text.RegularExpressions;
using CourtGo.Application.Auth;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Staff;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public partial class AdminStaffService(
    CourtGoDbContext db,
    IPasswordHasher passwordHasher,
    TimeProvider clock) : IAdminStaffService
{
    public async Task<PagedResult<AdminStaffSummaryDto>> GetStaffListAsync(AdminStaffQuery query, CancellationToken ct = default)
    {
        StaffOperationsService.ValidatePage(query.PageNumber, query.PageSize);

        var queryUsers = db.Users.AsNoTracking().Where(u => u.Role == UserRole.Staff);

        if (query.IsActive.HasValue)
        {
            queryUsers = queryUsers.Where(u => u.IsActive == query.IsActive.Value);
        }

        if (query.CenterId.HasValue)
        {
            queryUsers = queryUsers.Where(u => u.StaffAssignments.Any(sa => sa.IsActive && sa.SportCenterId == query.CenterId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (search.Length > 200)
                throw new ValidationException("Search exceeds 200 characters.");

            queryUsers = queryUsers.Where(u =>
                u.FullName.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)) ||
                u.PhoneNumber.Contains(search));
        }

        var total = await queryUsers.CountAsync(ct);

        var items = await queryUsers
            .OrderByDescending(u => u.CreatedAt)
            .ThenByDescending(u => u.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new AdminStaffSummaryDto(
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.IsActive,
                u.StaffAssignments.Where(sa => sa.IsActive)
                    .Select(sa => new AssignedCenterDto(sa.SportCenterId, sa.SportCenter!.Name))
                    .FirstOrDefault(),
                u.CreatedAt))
            .ToListAsync(ct);

        return new(items, query.PageNumber, query.PageSize, total, (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public async Task<AdminStaffDetailDto> GetStaffDetailAsync(Guid staffId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == staffId && u.Role == UserRole.Staff)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.IsActive,
                u.CreatedAt,
                ActiveAssignment = u.StaffAssignments.Where(sa => sa.IsActive)
                    .Select(sa => new AssignedCenterDto(sa.SportCenterId, sa.SportCenter!.Name))
                    .FirstOrDefault(),
                History = u.StaffAssignments
                    .OrderByDescending(sa => sa.AssignedAt)
                    .Select(sa => new StaffAssignmentHistoryDto(
                        sa.Id,
                        sa.SportCenterId,
                        sa.SportCenter!.Name,
                        sa.AssignedAt,
                        sa.IsActive))
                    .ToList()
            })
            .SingleOrDefaultAsync(ct);

        if (user is null)
            throw new NotFoundException("Staff member not found.");

        return new AdminStaffDetailDto(
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.IsActive,
            user.ActiveAssignment,
            user.History,
            user.CreatedAt);
    }

    public async Task<AdminStaffDetailDto> CreateStaffAsync(CreateStaffRequest request, CancellationToken ct = default)
    {
        ValidateFullName(request.FullName);
        var phone = ValidateAndNormalizePhone(request.PhoneNumber);
        var email = ValidateAndNormalizeEmail(request.Email);
        ValidatePassword(request.Password);

        var center = await db.SportCenters.AsNoTracking()
            .Where(sc => sc.Id == request.SportCenterId)
            .Select(sc => new { sc.Id, sc.Name, sc.Status })
            .SingleOrDefaultAsync(ct);

        if (center is null || center.Status != SportCenterStatus.Active)
            throw new ValidationException("Sport center not found or is inactive.");

        if (await db.Users.AnyAsync(u => u.PhoneNumber == phone, ct))
            throw new ConflictException("Phone number is already registered.", ErrorCodes.PhoneAlreadyExists);

        if (email != null && await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Email is already registered.", ErrorCodes.EmailAlreadyExists);

        var now = clock.GetUtcNow();

        var user = new User
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = phone,
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = UserRole.Staff, // Force Role = Staff
            IsActive = true,
            CreatedAt = now
        };

        var assignment = new StaffAssignment
        {
            StaffUserId = user.Id,
            SportCenterId = center.Id,
            IsActive = true,
            AssignedAt = now
        };

        user.StaffAssignments.Add(assignment);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var activeCenter = new AssignedCenterDto(center.Id, center.Name);
        var history = new List<StaffAssignmentHistoryDto>
        {
            new(assignment.Id, center.Id, center.Name, assignment.AssignedAt, true)
        };

        return new AdminStaffDetailDto(user.Id, user.FullName, user.Email, user.PhoneNumber, user.IsActive, activeCenter, history, user.CreatedAt);
    }

    public async Task<AdminStaffDetailDto> UpdateStaffProfileAsync(Guid staffId, UpdateStaffProfileRequest request, CancellationToken ct = default)
    {
        ValidateFullName(request.FullName);
        var phone = ValidateAndNormalizePhone(request.PhoneNumber);
        var email = ValidateAndNormalizeEmail(request.Email);

        var user = await db.Users
            .Include(u => u.StaffAssignments)
            .ThenInclude(sa => sa.SportCenter)
            .SingleOrDefaultAsync(u => u.Id == staffId && u.Role == UserRole.Staff, ct);

        if (user is null)
            throw new NotFoundException("Staff member not found.");

        if (user.PhoneNumber != phone && await db.Users.AnyAsync(u => u.PhoneNumber == phone && u.Id != staffId, ct))
            throw new ConflictException("Phone number is already registered.", ErrorCodes.PhoneAlreadyExists);

        if (email != null && user.Email != email && await db.Users.AnyAsync(u => u.Email == email && u.Id != staffId, ct))
            throw new ConflictException("Email is already registered.", ErrorCodes.EmailAlreadyExists);

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = phone;
        user.Email = email;
        user.UpdatedAt = clock.GetUtcNow();

        await db.SaveChangesAsync(ct);

        return ToDetailDto(user);
    }

    public async Task<AdminStaffDetailDto> UpdateStaffStatusAsync(Guid staffId, UpdateStaffStatusRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.StaffAssignments)
            .ThenInclude(sa => sa.SportCenter)
            .SingleOrDefaultAsync(u => u.Id == staffId && u.Role == UserRole.Staff, ct);

        if (user is null)
            throw new NotFoundException("Staff member not found.");

        user.IsActive = request.IsActive;

        // If deactivating staff, also deactivate any active staff assignment so no active center shifts remain.
        // Historical assignments are preserved.
        if (!request.IsActive)
        {
            foreach (var sa in user.StaffAssignments.Where(sa => sa.IsActive))
            {
                sa.IsActive = false;
            }
        }

        user.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);

        return ToDetailDto(user);
    }

    public async Task<AdminStaffDetailDto> ReassignStaffAsync(Guid staffId, ReassignStaffRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.StaffAssignments)
            .ThenInclude(sa => sa.SportCenter)
            .SingleOrDefaultAsync(u => u.Id == staffId && u.Role == UserRole.Staff, ct);

        if (user is null)
            throw new NotFoundException("Staff member not found.");

        var center = await db.SportCenters.AsNoTracking()
            .Where(sc => sc.Id == request.SportCenterId)
            .Select(sc => new { sc.Id, sc.Name, sc.Status })
            .SingleOrDefaultAsync(ct);

        if (center is null || center.Status != SportCenterStatus.Active)
            throw new ValidationException("Sport center not found or is inactive.");

        foreach (var sa in user.StaffAssignments.Where(sa => sa.IsActive))
        {
            sa.IsActive = false;
        }

        var now = clock.GetUtcNow();
        var newAssignment = new StaffAssignment
        {
            StaffUserId = staffId,
            SportCenterId = center.Id,
            IsActive = true,
            AssignedAt = now
        };

        db.StaffAssignments.Add(newAssignment);
        user.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        return ToDetailDto(user);
    }

    private static AdminStaffDetailDto ToDetailDto(User user)
    {
        var active = user.StaffAssignments
            .Where(sa => sa.IsActive)
            .Select(sa => new AssignedCenterDto(sa.SportCenterId, sa.SportCenter?.Name ?? string.Empty))
            .FirstOrDefault();

        var history = user.StaffAssignments
            .OrderByDescending(sa => sa.AssignedAt)
            .Select(sa => new StaffAssignmentHistoryDto(
                sa.Id,
                sa.SportCenterId,
                sa.SportCenter?.Name ?? string.Empty,
                sa.AssignedAt,
                sa.IsActive))
            .ToList();

        return new AdminStaffDetailDto(
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.IsActive,
            active,
            history,
            user.CreatedAt);
    }

    private static void ValidateFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ValidationException("Full name is required.");
        if (fullName.Trim().Length > 150)
            throw new ValidationException("Full name must be at most 150 characters.");
    }

    private static string ValidateAndNormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ValidationException("Phone number is required.");
        var normalized = PhoneNumberFormat.Normalize(phone);
        if (!PhoneNumberFormat.IsValid(normalized))
            throw new ValidationException("Phone number is invalid.");
        return normalized;
    }

    private static string? ValidateAndNormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;
        var trimmed = email.Trim().ToLowerInvariant();
        if (trimmed.Length > 255 || !StaffEmailRegex().IsMatch(trimmed))
            throw new ValidationException("Email is invalid.");
        return trimmed;
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ValidationException("Password is required.");
        if (password.Length < 8 || password.Length > 128 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            throw new ValidationException("Password must be 8-128 characters and contain uppercase, lowercase, and digits.");
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex StaffEmailRegex();
}
