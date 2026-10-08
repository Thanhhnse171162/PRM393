using System.Data;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Domain.Entities;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

/// <summary>Serializes booking commands in SQL Server, including across API instances.</summary>
public sealed class BookingCommandExecutor(CourtGoDbContext dbContext)
{
    private readonly CourtGoDbContext _dbContext = dbContext;
    // Bounded locks also support the existing EF InMemory test provider.
    private static readonly SemaphoreSlim[] Gates = Enumerable.Range(0, 256).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    internal async Task<List<BookingSlot>> ReloadSlotsAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.BookingSlots.AsNoTracking().Where(s => s.BookingId == bookingId)
            .ToListAsync(cancellationToken);
        return rows.Select(row =>
        {
            var tracked = _dbContext.BookingSlots.Local.FirstOrDefault(s => s.Id == row.Id);
            if (tracked is null)
            {
                _dbContext.Attach(row);
                return row;
            }
            _dbContext.Entry(tracked).CurrentValues.SetValues(row);
            return tracked;
        }).ToList();
    }

    public async Task<T> ExecuteAsync<T>(Guid bookingId, Func<Booking, CancellationToken, Task<T>> command,
        CancellationToken cancellationToken)
    {
        var gate = Gates[(uint)bookingId.GetHashCode() % (uint)Gates.Length];
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = _dbContext.Database.IsRelational()
                ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : null;
            try
            {
                // UPDLOCK prevents two readers from upgrading shared locks concurrently.
                // HOLDLOCK keeps this row protected until payment/history/check-in commit.
                var query = _dbContext.Database.IsSqlServer()
                    ? _dbContext.Bookings.FromSqlInterpolated(
                        $"SELECT * FROM [Bookings] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {bookingId}")
                    : _dbContext.Bookings.Where(b => b.Id == bookingId);
                var booking = await query.SingleOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException("Booking was not found.", ErrorCodes.BookingNotFound);
                await _dbContext.Entry(booking).ReloadAsync(cancellationToken);
                var result = await command(booking, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(CancellationToken.None);
                _dbContext.ChangeTracker.Clear();
                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }
}
