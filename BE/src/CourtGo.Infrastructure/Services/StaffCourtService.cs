using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Operations;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace CourtGo.Infrastructure.Services;
public class StaffCourtService(CourtGoDbContext db, TimeProvider clock) : IStaffCourtService
{
    public async Task<PagedResult<CourtStateDto>> GetCourtsAsync(Guid staffId, int pageNumber, int pageSize, CancellationToken ct)
    {
        StaffOperationsService.ValidatePage(pageNumber, pageSize);
        var center = await StaffAccess.GetCenterAsync(db, staffId, ct);
        var service = new StaffOperationsService(db, clock);
        var count = await db.Courts.CountAsync(c => c.SportCenterId == center, ct);
        var states = await service.CourtStatesAsync(center, ct, pageNumber, pageSize);
        return new(states, pageNumber, pageSize, count, (int)Math.Ceiling(count / (double)pageSize));
    }
    public async Task<CourtStateDto> GetCourtAsync(Guid staffId, Guid courtId, CancellationToken ct)
    {
        var center = await ValidateCourtAsync(staffId, courtId, ct);
        return (await new StaffOperationsService(db, clock).CourtStatesAsync(center, ct, courtId: courtId)).Single();
    }
    public async Task<PagedResult<CourtBlockDto>> GetBlocksAsync(Guid staffId, Guid courtId, int pageNumber, int pageSize, CancellationToken ct)
    {
        await ValidateCourtAsync(staffId, courtId, ct);
        StaffOperationsService.ValidatePage(pageNumber, pageSize);
        var query = db.CourtBlocks.AsNoTracking().Where(b => b.CourtId == courtId);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(b => b.StartAt).ThenBy(b => b.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(b => new CourtBlockDto(b.Id, b.CourtId, b.StartAt, b.EndAt, b.Type.ToString(), b.Reason)).ToListAsync(ct);
        return new(items, pageNumber, pageSize, count, (int)Math.Ceiling(count / (double)pageSize));
    }
    public async Task<CourtBlockDto> CreateBlockAsync(Guid staffId, Guid courtId, CreateCourtBlockRequest request, CancellationToken ct)
    {
        await using var lease = await CourtMutationLease.AcquireAsync(db, courtId, ct);
        await ValidateCourtAsync(staffId, courtId, ct);
        var now = clock.GetUtcNow();
        if (request.EndAt <= request.StartAt || request.StartAt < now || request.EndAt > now.AddYears(2))
            throw new ValidationException("Block must start now or later, end after its start, and be within two years.");
        if (!Enum.TryParse<CourtBlockType>(request.Type, true, out var type) || !Enum.IsDefined(type))
            throw new ValidationException("Invalid block type.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
            throw new ValidationException("Reason is required and must not exceed 500 characters.");
        if (await db.BookingSlots.AnyAsync(s => s.CourtId == courtId && s.IsOccupying && s.StartAt < request.EndAt && s.EndAt > request.StartAt &&
            (s.ReservationState == ReservationState.Reserved || s.HoldExpiresAt > now), ct))
            throw new ConflictException("Block overlaps a reservation or active hold.", ErrorCodes.BookingSlotConflict);
        if (await db.CourtBlocks.AnyAsync(b => b.CourtId == courtId && b.StartAt < request.EndAt && b.EndAt > request.StartAt, ct))
            throw new ConflictException("Block overlaps an existing block.");
        var block = new CourtBlock { CourtId = courtId, StartAt = request.StartAt, EndAt = request.EndAt, Type = type,
            Reason = request.Reason.Trim(), CreatedByUserId = staffId, CreatedAt = now };
        db.CourtBlocks.Add(block);
        await db.SaveChangesAsync(ct);
        await lease.CommitAsync(ct);
        return new(block.Id, courtId, block.StartAt, block.EndAt, type.ToString(), block.Reason);
    }
    public async Task RemoveBlockAsync(Guid staffId, Guid courtId, Guid blockId, CancellationToken ct)
    {
        await using var lease = await CourtMutationLease.AcquireAsync(db, courtId, ct);
        await ValidateCourtAsync(staffId, courtId, ct);
        var block = await db.CourtBlocks.SingleOrDefaultAsync(b => b.Id == blockId && b.CourtId == courtId, ct)
            ?? throw new NotFoundException("Block not found.");
        var now = clock.GetUtcNow();
        if (block.EndAt <= now) throw new ConflictException("Historical blocks cannot be removed.");
        // Schema has no soft-delete flag. Preserve elapsed history by ending an active block now.
        if (block.StartAt < now) block.EndAt = now;
        else db.CourtBlocks.Remove(block);
        await db.SaveChangesAsync(ct);
        await lease.CommitAsync(ct);
    }
    private async Task<Guid> ValidateCourtAsync(Guid staffId, Guid courtId, CancellationToken ct)
    {
        var center = await StaffAccess.GetCenterAsync(db, staffId, ct);
        if (!await db.Courts.AnyAsync(c => c.Id == courtId && c.SportCenterId == center, ct))
            throw new NotFoundException("Court not found.", ErrorCodes.CourtNotFound);
        return center;
    }
}
