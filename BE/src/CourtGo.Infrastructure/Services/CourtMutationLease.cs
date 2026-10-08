using System.Data;
using CourtGo.Domain.Entities;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace CourtGo.Infrastructure.Services;

/// <summary>Shared lock order for hold, walk-in, blocks and price rules: court before writes.</summary>
internal sealed class CourtMutationLease : IAsyncDisposable
{
    private static readonly SemaphoreSlim[] Gates = Enumerable.Range(0, 256).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
    private readonly SemaphoreSlim gate;
    private readonly IDbContextTransaction? transaction;
    private CourtMutationLease(SemaphoreSlim gate, IDbContextTransaction? transaction) { this.gate = gate; this.transaction = transaction; }
    public static async Task<CourtMutationLease> AcquireAsync(CourtGoDbContext db, Guid courtId, CancellationToken ct)
    {
        var gate = Gates[(uint)courtId.GetHashCode() % (uint)Gates.Length];
        await gate.WaitAsync(ct);
        IDbContextTransaction? transaction = null;
        try
        {
            if (db.Database.IsRelational()) transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            if (db.Database.IsSqlServer())
                await db.Courts.FromSqlInterpolated($"SELECT * FROM [Courts] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {courtId}")
                    .AsNoTracking().ToListAsync(ct);
            return new(gate, transaction);
        }
        catch { if (transaction is not null) await transaction.DisposeAsync(); gate.Release(); throw; }
    }
    public async Task CommitAsync(CancellationToken ct) { if (transaction is not null) await transaction.CommitAsync(ct); }
    public async ValueTask DisposeAsync()
    {
        try { if (transaction is not null) await transaction.DisposeAsync(); }
        finally { gate.Release(); }
    }
}
