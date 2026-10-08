using CourtGo.Application.Common.Exceptions;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace CourtGo.Infrastructure.Services;
internal static class StaffAccess
{
    internal static async Task<Guid> GetCenterAsync(CourtGoDbContext db, Guid staffId, CancellationToken ct)
    {
        var center = await db.StaffAssignments.AsNoTracking()
            .Where(a => a.StaffUserId == staffId && a.IsActive && a.StaffUser!.IsActive && a.StaffUser.Role == UserRole.Staff)
            .Select(a => (Guid?)a.SportCenterId).SingleOrDefaultAsync(ct);
        return center ?? throw new ForbiddenException("Staff has no active center assignment.", ErrorCodes.StaffNotAssigned);
    }
}
