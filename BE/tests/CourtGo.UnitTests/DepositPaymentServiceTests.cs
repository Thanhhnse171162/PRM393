using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using CourtGo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.UnitTests;

public class DepositPaymentServiceTests
{
    private sealed class Fixture : IDisposable
    {
        public CourtGoDbContext Db { get; }
        public FakeTimeProvider Clock { get; } = new() { Now = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        public Booking Booking { get; }
        public DepositPaymentService Service { get; }
        public DevelopmentPaymentSimulationService Simulation { get; }
        public ExpiredBookingHoldService Cleanup { get; }
        public Fixture()
        {
            Db = new(new DbContextOptionsBuilder<CourtGoDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var executor = new BookingCommandExecutor(Db);
            Cleanup = new(Db, executor, Clock);
            var gateway = new DevelopmentPaymentGateway();
            Service = new(Db, executor, Cleanup, gateway, new QrTokenGenerator(), Clock);
            Simulation = new(Db, gateway, Service);
            Booking = new Booking
            {
                CustomerUserId = Guid.NewGuid(), CourtId = Guid.NewGuid(), BookingCode = "DEPOSIT-TEST",
                StartAt = Clock.Now.AddDays(1), EndAt = Clock.Now.AddDays(1).AddHours(2), DurationMinutes = 120,
                BookingStatus = BookingStatus.PendingPayment, PaymentStatus = BookingPaymentStatus.Unpaid,
                TotalAmount = 200000, DepositAmount = 60000, DepositPercentSnapshot = 30,
                HoldExpiresAt = Clock.Now.AddMinutes(10), CustomerNameSnapshot = "Customer",
                CourtNameSnapshot = "Original court", CenterNameSnapshot = "Original center", SportNameSnapshot = "Original sport"
            };
            Booking.Slots = Enumerable.Range(0, 2).Select(i => new BookingSlot
            {
                CourtId = Booking.CourtId, StartAt = Booking.StartAt.AddHours(i), EndAt = Booking.StartAt.AddHours(i + 1),
                UnitPrice = 100000, IsOccupying = true, ReservationState = ReservationState.Held,
                HoldExpiresAt = Booking.HoldExpiresAt
            }).ToList();
            Db.Bookings.Add(Booking);
            Db.SaveChanges();
        }
        public Task<DepositPaymentResponse> StartAsync(string method = "MoMo") =>
            Service.StartDepositAsync(Booking.CustomerUserId!.Value, Booking.Id, new(method));
        public Task<DepositConfirmationResponse> SimulateAsync(Guid paymentId, string result = "success") =>
            Simulation.SimulateAsync(Booking.CustomerUserId!.Value, paymentId, new(result));
        public void Dispose() => Db.Dispose();
    }

    [Theory]
    [InlineData("MoMo")]
    [InlineData("VNPay")]
    public async Task Start_CreatesPendingFromSnapshot_AndDuplicateReturnsSameAttempt(string method)
    {
        using var f = new Fixture();
        f.Booking.DepositAmount = 54321; // Historical snapshot, not a recalculated percentage.
        await f.Db.SaveChangesAsync();
        var first = await f.StartAsync(method);
        var second = await f.StartAsync(method);
        Assert.Equal(first.PaymentId, second.PaymentId);
        Assert.Equal(54321, first.Amount);
        Assert.Equal("Pending", first.TransactionStatus);
        Assert.Equal("Development", first.Gateway);
        Assert.StartsWith("dev-", first.ProviderTransactionId);
        Assert.Null(first.PaymentUrl);
        var payment = Assert.Single(await f.Db.Payments.ToListAsync());
        Assert.Equal(PaymentKind.Deposit, payment.PaymentKind);
        Assert.Equal(f.Booking.CustomerUserId, payment.PaidByUserId);
        Assert.Null(payment.ConfirmedByUserId);
        Assert.Null(payment.PaidAt);
        Assert.Equal(BookingStatus.PendingPayment, f.Booking.BookingStatus);
        Assert.Equal(BookingPaymentStatus.Unpaid, f.Booking.PaymentStatus);
        Assert.Null(f.Booking.QrToken);
        Assert.All(f.Booking.Slots, s => Assert.Equal(ReservationState.Held, s.ReservationState));
    }

    [Fact]
    public async Task Ownership_IsEnforcedForStartStatusAndSimulation()
    {
        using var f = new Fixture();
        var other = Guid.NewGuid();
        Assert.Equal(ErrorCodes.BookingNotFound, (await Assert.ThrowsAsync<NotFoundException>(() =>
            f.Service.StartDepositAsync(other, f.Booking.Id, new("MoMo")))).Code);
        Assert.Equal(ErrorCodes.BookingNotFound, (await Assert.ThrowsAsync<NotFoundException>(() =>
            f.Service.GetPaymentStatusAsync(other, f.Booking.Id))).Code);
        var payment = await f.StartAsync();
        Assert.Equal(ErrorCodes.PaymentNotFound, (await Assert.ThrowsAsync<NotFoundException>(() =>
            f.Simulation.SimulateAsync(other, payment.PaymentId, new("success")))).Code);
        Assert.Equal(ErrorCodes.PaymentNotFound, (await Assert.ThrowsAsync<NotFoundException>(() =>
            f.SimulateAsync(Guid.NewGuid()))).Code);
    }

    [Theory]
    [InlineData("Cash")]
    [InlineData("BankTransfer")]
    [InlineData("Other")]
    [InlineData("3")]
    [InlineData("")]
    public async Task UnsupportedMethods_DoNotCreatePayment(string method)
    {
        using var f = new Fixture();
        Assert.Equal(ErrorCodes.UnsupportedPaymentMethod,
            (await Assert.ThrowsAsync<ValidationException>(() => f.StartAsync(method))).Code);
        Assert.Empty(await f.Db.Payments.ToListAsync());
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("null")]
    [InlineData("slotExpired")]
    [InlineData("slotNull")]
    [InlineData("released")]
    public async Task InvalidHold_IsRejected(string invalid)
    {
        using var f = new Fixture();
        switch (invalid)
        {
            case "expired": f.Booking.HoldExpiresAt = f.Clock.Now; break;
            case "null": f.Booking.HoldExpiresAt = null; break;
            case "slotExpired": f.Booking.Slots.First().HoldExpiresAt = f.Clock.Now; break;
            case "slotNull": f.Booking.Slots.First().HoldExpiresAt = null; break;
            case "released": f.Booking.Slots.First().ReservationState = ReservationState.Released; break;
        }
        await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorCodes.BookingHoldExpired, (await Assert.ThrowsAsync<ConflictException>(() => f.StartAsync())).Code);
        Assert.Empty(await f.Db.Payments.ToListAsync());
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("notOccupying")]
    [InlineData("wrongCourt")]
    [InlineData("reserved")]
    public async Task InconsistentSlots_RejectConfirmation(string invalid)
    {
        using var f = new Fixture();
        var payment = await f.StartAsync();
        switch (invalid)
        {
            case "empty": f.Db.BookingSlots.RemoveRange(f.Booking.Slots); break;
            case "notOccupying": f.Booking.Slots.First().IsOccupying = false; break;
            case "wrongCourt": f.Booking.Slots.First().CourtId = Guid.NewGuid(); break;
            case "reserved": f.Booking.Slots.First().ReservationState = ReservationState.Reserved; break;
        }
        await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorCodes.PaymentStateConflict,
            (await Assert.ThrowsAsync<ConflictException>(() => f.SimulateAsync(payment.PaymentId))).Code);
        Assert.Equal(PaymentTransactionStatus.Pending, (await f.Db.Payments.SingleAsync()).TransactionStatus);
    }

    [Fact]
    public async Task Success_UpdatesEveryState_AndIsIdempotent()
    {
        using var f = new Fixture();
        var payment = await f.StartAsync();
        var slotIds = f.Booking.Slots.Select(s => s.Id).ToArray();
        var first = await f.SimulateAsync(payment.PaymentId);
        var qr = (await f.Db.Bookings.SingleAsync()).QrToken;
        var second = await f.SimulateAsync(payment.PaymentId);
        Assert.Equal(first, second);
        Assert.Equal("Succeeded", first.TransactionStatus);
        Assert.Equal("Confirmed", first.BookingStatus);
        Assert.Equal("DepositPaid", first.PaymentStatus);
        Assert.Equal(60000, first.DepositPaid);
        Assert.Equal(140000, first.RemainingAmount);
        var booking = await f.Db.Bookings.Include(b => b.Slots).SingleAsync();
        Assert.Null(booking.HoldExpiresAt);
        Assert.Equal(64, qr!.Length);
        Assert.NotEqual(booking.Id.ToString(), qr);
        Assert.Equal(qr, booking.QrToken);
        Assert.Equal(slotIds.OrderBy(i => i), booking.Slots.Select(s => s.Id).OrderBy(i => i));
        Assert.All(booking.Slots, s =>
        {
            Assert.Equal(ReservationState.Reserved, s.ReservationState);
            Assert.True(s.IsOccupying);
            Assert.Null(s.HoldExpiresAt);
            Assert.Equal(100000, s.UnitPrice);
        });
        var persisted = await f.Db.Payments.SingleAsync();
        Assert.Equal(f.Clock.Now, persisted.PaidAt);
        Assert.Null(persisted.ConfirmedByUserId);
        var history = await f.Db.BookingStatusHistories.SingleAsync();
        Assert.Equal(BookingStatus.PendingPayment, history.FromStatus);
        Assert.Equal(BookingStatus.Confirmed, history.ToStatus);
        Assert.Equal(f.Booking.CustomerUserId, history.ChangedByUserId);
        Assert.Single(await f.Db.Notifications.ToListAsync());
        Assert.Equal(ErrorCodes.PaymentAlreadyCompleted,
            (await Assert.ThrowsAsync<ConflictException>(() => f.StartAsync())).Code);
    }

    [Theory]
    [InlineData("failed", PaymentTransactionStatus.Failed)]
    [InlineData("cancelled", PaymentTransactionStatus.Cancelled)]
    public async Task FailureAndCancellation_KeepHold_AndAllowRetry(string result, PaymentTransactionStatus expected)
    {
        using var f = new Fixture();
        var payment = await f.StartAsync();
        await f.SimulateAsync(payment.PaymentId, result);
        await f.SimulateAsync(payment.PaymentId, result); // terminal result repeats safely
        Assert.Equal(expected, (await f.Db.Payments.SingleAsync()).TransactionStatus);
        Assert.Equal(BookingStatus.PendingPayment, (await f.Db.Bookings.SingleAsync()).BookingStatus);
        Assert.Equal(BookingPaymentStatus.Unpaid, f.Booking.PaymentStatus);
        Assert.Null(f.Booking.QrToken);
        Assert.All(f.Booking.Slots, s => { Assert.Equal(ReservationState.Held, s.ReservationState); Assert.True(s.IsOccupying); });
        Assert.Empty(await f.Db.BookingStatusHistories.ToListAsync());
        Assert.Empty(await f.Db.Notifications.ToListAsync());
        Assert.Equal(ErrorCodes.PaymentAlreadyFinalized,
            (await Assert.ThrowsAsync<ConflictException>(() => f.SimulateAsync(payment.PaymentId))).Code);
        var retry = await f.StartAsync();
        Assert.NotEqual(payment.PaymentId, retry.PaymentId);
        Assert.Equal(2, await f.Db.Payments.CountAsync());
        await f.SimulateAsync(retry.PaymentId);
        Assert.Equal(BookingStatus.Confirmed, (await f.Db.Bookings.SingleAsync()).BookingStatus);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LateSuccess_CannotConfirmOrResurrectReleasedSlots(bool cleanupFirst)
    {
        using var f = new Fixture();
        var payment = await f.StartAsync();
        f.Clock.Now = f.Clock.Now.AddMinutes(10); // exact expiry boundary is invalid
        if (cleanupFirst) await f.Cleanup.ReleaseExpiredHoldsAsync();
        Assert.Equal(ErrorCodes.PaymentAfterHoldExpired,
            (await Assert.ThrowsAsync<ConflictException>(() => f.SimulateAsync(payment.PaymentId))).Code);
        var booking = await f.Db.Bookings.Include(b => b.Slots).SingleAsync();
        Assert.NotEqual(BookingStatus.Confirmed, booking.BookingStatus);
        Assert.Null(booking.QrToken);
        Assert.DoesNotContain(booking.Slots, s => s.ReservationState == ReservationState.Reserved);
        Assert.Equal(PaymentTransactionStatus.Pending, (await f.Db.Payments.SingleAsync()).TransactionStatus);
        await f.Cleanup.ReleaseExpiredHoldsAsync();
        Assert.All(await f.Db.BookingSlots.ToListAsync(), s =>
        {
            Assert.Equal(ReservationState.Released, s.ReservationState);
            Assert.False(s.IsOccupying);
        });
    }

    [Fact]
    public async Task SuccessBeforeExpiry_IsNotExpiredByLaterCleanup_AndReplayDoesNotRewindCheckIn()
    {
        using var f = new Fixture();
        var payment = await f.StartAsync();
        await f.SimulateAsync(payment.PaymentId);
        var qr = f.Booking.QrToken;
        f.Clock.Now = f.Clock.Now.AddHours(1);
        await f.Cleanup.ReleaseExpiredHoldsAsync();
        Assert.Equal(BookingStatus.Confirmed, (await f.Db.Bookings.SingleAsync()).BookingStatus);
        f.Booking.BookingStatus = BookingStatus.CheckedIn;
        f.Booking.PaymentStatus = BookingPaymentStatus.FullyPaid;
        await f.Db.SaveChangesAsync();
        Assert.Equal("CheckedIn", (await f.SimulateAsync(payment.PaymentId)).BookingStatus);
        Assert.Equal(qr, f.Booking.QrToken);
        Assert.Single(await f.Db.BookingStatusHistories.ToListAsync());
    }

    [Theory]
    [InlineData("amount", ErrorCodes.PaymentAmountMismatch)]
    [InlineData("reference", ErrorCodes.PaymentVerificationFailed)]
    [InlineData("kind", ErrorCodes.PaymentStateConflict)]
    [InlineData("pendingResult", ErrorCodes.PaymentVerificationFailed)]
    public async Task Confirmation_RechecksTrustedResultAgainstDatabase(string invalid, string code)
    {
        using var f = new Fixture();
        var started = await f.StartAsync();
        var result = new VerifiedPaymentResult(started.PaymentId, started.ProviderTransactionId!, started.Amount, PaymentTransactionStatus.Succeeded);
        if (invalid == "amount") result = result with { Amount = 1 };
        if (invalid == "reference") result = result with { ProviderTransactionId = "unverified" };
        if (invalid == "pendingResult") result = result with { TransactionStatus = PaymentTransactionStatus.Pending };
        if (invalid == "kind")
        {
            (await f.Db.Payments.SingleAsync()).PaymentKind = PaymentKind.Remaining;
            await f.Db.SaveChangesAsync();
        }
        var error = await Assert.ThrowsAnyAsync<AppException>(() => f.Service.ApplyVerifiedResultAsync(result));
        Assert.Equal(code, error.Code);
        Assert.Null((await f.Db.Bookings.SingleAsync()).QrToken);
        Assert.Empty(await f.Db.BookingStatusHistories.ToListAsync());
    }

    [Fact]
    public async Task InvalidSimulationResult_AndDuplicateSucceededDeposit_AreRejected()
    {
        using var f = new Fixture();
        var payment = await f.StartAsync();
        Assert.Equal(ErrorCodes.PaymentVerificationFailed,
            (await Assert.ThrowsAsync<ValidationException>(() => f.SimulateAsync(payment.PaymentId, "paid"))).Code);
        f.Db.Payments.Add(new Payment { BookingId = f.Booking.Id, PaymentKind = PaymentKind.Deposit,
            TransactionStatus = PaymentTransactionStatus.Succeeded, Amount = 60000 });
        await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorCodes.PaymentAlreadyCompleted,
            (await Assert.ThrowsAsync<ConflictException>(() => f.StartAsync())).Code);
        Assert.Equal(ErrorCodes.PaymentAlreadyCompleted,
            (await Assert.ThrowsAsync<ConflictException>(() => f.SimulateAsync(payment.PaymentId))).Code);
    }

    [Fact]
    public async Task AlreadyConfirmedWithoutSuccessRecord_CannotStartDeposit()
    {
        using var f = new Fixture();
        f.Booking.BookingStatus = BookingStatus.Confirmed;
        await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorCodes.BookingNotPendingPayment,
            (await Assert.ThrowsAsync<ConflictException>(() => f.StartAsync())).Code);
    }

    [Fact]
    public void QrTokens_Have256Bits_AndAreDistinct()
    {
        var generator = new QrTokenGenerator();
        var tokens = Enumerable.Range(0, 100).Select(_ => generator.GenerateToken()).ToList();
        Assert.Equal(100, tokens.Distinct().Count());
        Assert.All(tokens, token => Assert.Equal(32, Convert.FromHexString(token).Length));
    }
}
