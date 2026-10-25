using Microsoft.Extensions.Caching.Memory;

namespace CourtGo.Infrastructure.Payments;

/// <summary>Bounded, process-local cooldown. Database locking still owns finalization across processes.</summary>
public sealed class PaymentReconciliationGate : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 2048 });
    private readonly SemaphoreSlim[] gates = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
    public SemaphoreSlim For(Guid id) => gates[(id.GetHashCode() & int.MaxValue) % gates.Length];
    public bool TryBegin(Guid id)
    {
        if (cache.TryGetValue(id, out _)) return false;
        // VNPAY QueryDR rejects repeated transaction queries within five minutes (code 94).
        cache.Set(id, true, new MemoryCacheEntryOptions().SetSize(1).SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
        return true;
    }
    public void Dispose() { cache.Dispose(); foreach (var gate in gates) gate.Dispose(); }
}
