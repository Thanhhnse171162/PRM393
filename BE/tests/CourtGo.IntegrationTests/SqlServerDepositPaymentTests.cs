using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using CourtGo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Fixture = CourtGo.IntegrationTests.SqlServerBookingWorkflowTests.Fixture;

namespace CourtGo.IntegrationTests;

[Collection("SqlServer workflow")]
public class SqlServerDepositPaymentTests
{
    private sealed class Clock : TimeProvider
    {
        // Keeps cleanup from touching normal application holds while testing on an existing schema.
        public DateTimeOffset Now { get; set; } = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class AfterSave(BookingStatus status, bool fail = false) : SaveChangesInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _triggered;
        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (!_triggered && eventData.Context!.ChangeTracker.Entries<Booking>().Any(e => e.Entity.BookingStatus == status))
            {
                _triggered = true;
                Entered.TrySetResult();
                if (fail) throw new InvalidOperationException("Injected failure after database writes, before commit.");
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }

    private sealed class DuplicateReferenceGateway(string reference) : IPaymentGateway
    {
        public string Name => "Test";
        public bool Supports(PaymentMethod method) => true;
        public Task<GatewayPaymentAttempt> CreateDepositAsync(GatewayPaymentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new GatewayPaymentAttempt(reference, null, Name));
    }

    private static ExpiredBookingHoldService Cleanup(CourtGoDbContext db, TimeProvider clock)
        => new(db, new BookingCommandExecutor(db), clock);
    private static DepositPaymentService Deposit(CourtGoDbContext db, TimeProvider clock, IPaymentGateway? gateway = null)
        => new(db, new BookingCommandExecutor(db), Cleanup(db, clock), gateway ?? new DevelopmentPaymentGateway(),
            new QrTokenGenerator(), clock);
    private static DevelopmentPaymentSimulationService Simulator(CourtGoDbContext db, TimeProvider clock)
        => new(db, new DevelopmentPaymentGateway(), Deposit(db, clock));
    private static async Task<DepositPaymentResponse> StartAsync(Fixture fixture, TimeProvider clock)
    {
        await using var db = fixture.Open();
        return await Deposit(db, clock).StartDepositAsync(fixture.CustomerId, fixture.BookingId, new("MoMo"));
    }

    [SqlServerFact]
    public async Task ConcurrentDepositStartsAndSuccess_CommitOnce_AndPaymentStatusProjectsInSql()
    {
        var clock = new Clock();
        await using var fixture = await Fixture.CreateAsync(held: true, clock);
        var attempts = await Task.WhenAll(StartAsync(fixture, clock), StartAsync(fixture, clock));
        Assert.Equal(attempts[0].PaymentId, attempts[1].PaymentId);
        async Task<DepositConfirmationResponse> ConfirmAsync()
        {
            await using var db = fixture.Open();
            return await Simulator(db, clock).SimulateAsync(fixture.CustomerId, attempts[0].PaymentId, new("success"));
        }
        var successes = await Task.WhenAll(ConfirmAsync(), ConfirmAsync());
        Assert.Equal(successes[0], successes[1]);
        await using var check = fixture.Open();
        Assert.Equal(1, await check.Payments.CountAsync(p => p.BookingId == fixture.BookingId));
        Assert.Equal(1, await check.BookingStatusHistories.CountAsync(h => h.BookingId == fixture.BookingId));
        Assert.Equal(1, await check.Notifications.CountAsync(n => n.ReferenceId == fixture.BookingId));
        var booking = await check.Bookings.AsNoTracking().Include(b => b.Slots).SingleAsync(b => b.Id == fixture.BookingId);
        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus);
        Assert.Null(booking.HoldExpiresAt);
        Assert.False(string.IsNullOrWhiteSpace(booking.QrToken));
        Assert.All(booking.Slots, s =>
        {
            Assert.Equal(ReservationState.Reserved, s.ReservationState);
            Assert.Null(s.HoldExpiresAt);
            Assert.True(s.IsOccupying);
        });
        var status = await Deposit(check, clock).GetPaymentStatusAsync(fixture.CustomerId, fixture.BookingId);
        Assert.Equal(60000, status.PaidAmount);
        Assert.Equal(140000, status.RemainingAmount);
        Assert.Equal("Succeeded", Assert.Single(status.Payments).TransactionStatus);
    }

    [SqlServerFact]
    public async Task FailedCommit_RollsBackPaymentBookingSlotsQrHistoryAndNotification()
    {
        var clock = new Clock();
        await using var fixture = await Fixture.CreateAsync(held: true, clock);
        var payment = await StartAsync(fixture, clock);
        await using (var failing = fixture.Open(new AfterSave(BookingStatus.Confirmed, fail: true)))
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Simulator(failing, clock).SimulateAsync(fixture.CustomerId, payment.PaymentId, new("success")));
        await using var check = fixture.Open();
        var booking = await check.Bookings.AsNoTracking().Include(b => b.Slots).SingleAsync(b => b.Id == fixture.BookingId);
        Assert.Equal(BookingStatus.PendingPayment, booking.BookingStatus);
        Assert.Equal(BookingPaymentStatus.Unpaid, booking.PaymentStatus);
        Assert.Null(booking.QrToken);
        Assert.NotNull(booking.HoldExpiresAt);
        Assert.All(booking.Slots, s => { Assert.Equal(ReservationState.Held, s.ReservationState); Assert.NotNull(s.HoldExpiresAt); });
        var persisted = await check.Payments.SingleAsync(p => p.Id == payment.PaymentId);
        Assert.Equal(PaymentTransactionStatus.Pending, persisted.TransactionStatus);
        Assert.Null(persisted.PaidAt);
        Assert.False(await check.BookingStatusHistories.AnyAsync(h => h.BookingId == fixture.BookingId));
        Assert.False(await check.Notifications.AnyAsync(n => n.ReferenceId == fixture.BookingId));
        // A retry after rollback must still be a valid first confirmation.
        Assert.Equal("Confirmed", (await Simulator(check, clock).SimulateAsync(fixture.CustomerId, payment.PaymentId, new("success"))).BookingStatus);
    }

    [SqlServerFact]
    public async Task DuplicateProviderReference_ReturnsSafeConflict_AndRollsBackNewAttempt()
    {
        var clock = new Clock();
        await using var fixture = await Fixture.CreateAsync(held: true, clock);
        var reference = "duplicate-test-" + Guid.NewGuid().ToString("N");
        await using var db = fixture.Open();
        db.Payments.Add(new Payment { BookingId = fixture.BookingId, PaymentKind = PaymentKind.Deposit,
            PaymentMethod = PaymentMethod.MoMo, Amount = 60000, TransactionStatus = PaymentTransactionStatus.Failed,
            ProviderTransactionId = reference });
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            Deposit(db, clock, new DuplicateReferenceGateway(reference)).StartDepositAsync(
                fixture.CustomerId, fixture.BookingId, new("MoMo")));
        Assert.Equal(ErrorCodes.PaymentStateConflict, error.Code);
        Assert.DoesNotContain("SQL", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await db.Payments.CountAsync(p => p.BookingId == fixture.BookingId));
        Assert.Equal(BookingStatus.PendingPayment, (await db.Bookings.SingleAsync(b => b.Id == fixture.BookingId)).BookingStatus);
    }

    [SqlServerFact]
    public async Task ConfirmationWins_ConcurrentExpiryCannotReleaseReservedSlots()
    {
        var clock = new Clock();
        await using var fixture = await Fixture.CreateAsync(held: true, clock);
        var payment = await StartAsync(fixture, clock);
        var pause = new AfterSave(BookingStatus.Confirmed);
        await using var confirmationDb = fixture.Open(pause);
        await using var cleanupDb = fixture.Open();
        var confirmation = Simulator(confirmationDb, clock).SimulateAsync(fixture.CustomerId, payment.PaymentId, new("success"));
        await pause.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        clock.Now = clock.Now.AddMinutes(11);
        var cleanup = Cleanup(cleanupDb, clock).ReleaseExpiredHoldsAsync();
        try
        {
            Assert.NotSame(cleanup, await Task.WhenAny(cleanup, Task.Delay(150)));
        }
        finally { pause.Release.TrySetResult(); }
        await Task.WhenAll(confirmation, cleanup);
        var booking = await cleanupDb.Bookings.AsNoTracking().Include(b => b.Slots).SingleAsync(b => b.Id == fixture.BookingId);
        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus);
        Assert.All(booking.Slots, s => { Assert.True(s.IsOccupying); Assert.Equal(ReservationState.Reserved, s.ReservationState); });
        Assert.False(await cleanupDb.BookingStatusHistories.AnyAsync(h => h.BookingId == fixture.BookingId && h.ToStatus == BookingStatus.Expired));
    }

    [SqlServerFact]
    public async Task ExpiryWins_ConcurrentCallbackCannotRestoreReleasedSlots()
    {
        var clock = new Clock();
        await using var fixture = await Fixture.CreateAsync(held: true, clock);
        var payment = await StartAsync(fixture, clock);
        clock.Now = clock.Now.AddMinutes(11);
        var pause = new AfterSave(BookingStatus.Expired);
        await using var cleanupDb = fixture.Open(pause);
        await using var confirmationDb = fixture.Open();
        var cleanup = Cleanup(cleanupDb, clock).ReleaseExpiredHoldsAsync();
        await pause.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var confirmation = Simulator(confirmationDb, clock).SimulateAsync(fixture.CustomerId, payment.PaymentId, new("success"));
        try
        {
            Assert.NotSame(confirmation, await Task.WhenAny(confirmation, Task.Delay(150)));
        }
        finally { pause.Release.TrySetResult(); }
        await cleanup;
        var error = await Assert.ThrowsAsync<ConflictException>(() => confirmation);
        Assert.Equal(ErrorCodes.PaymentAfterHoldExpired, error.Code);
        var booking = await confirmationDb.Bookings.AsNoTracking().Include(b => b.Slots).SingleAsync(b => b.Id == fixture.BookingId);
        Assert.Equal(BookingStatus.Expired, booking.BookingStatus);
        Assert.Null(booking.QrToken);
        Assert.All(booking.Slots, s => { Assert.False(s.IsOccupying); Assert.Equal(ReservationState.Released, s.ReservationState); });
        Assert.Equal(PaymentTransactionStatus.Pending, (await confirmationDb.Payments.SingleAsync(p => p.Id == payment.PaymentId)).TransactionStatus);
        Assert.Equal(1, await confirmationDb.BookingStatusHistories.CountAsync(h => h.BookingId == fixture.BookingId && h.ToStatus == BookingStatus.Expired));
        Assert.False(await confirmationDb.Notifications.AnyAsync(n => n.ReferenceId == fixture.BookingId));
    }
}
