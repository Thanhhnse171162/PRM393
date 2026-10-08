using System.Data.Common;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using CourtGo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CourtGo.IntegrationTests;

// Opt in against an existing schema only. Never creates/alters a database or runs migrations.
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COURTGO_TEST_SQLSERVER")))
            Skip = "Set COURTGO_TEST_SQLSERVER to an existing CourtGo schema to run SQL Server transaction tests.";
    }
}

[CollectionDefinition("SqlServer workflow", DisableParallelization = true)]
public sealed class SqlServerWorkflowCollection { }

[Collection("SqlServer workflow")]
public class SqlServerBookingWorkflowTests
{
    private sealed class SqlCapture : DbCommandInterceptor
    {
        public List<string> Commands { get; } = new();
        public TaskCompletionSource LockQueryStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            if (command.CommandText.Contains("UPDLOCK"))
                LockQueryStarted.TrySetResult();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FailSave(bool payment) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var db = eventData.Context!;
            if (payment
                ? db.ChangeTracker.Entries<Booking>().Any(e => e.Entity.PaymentStatus == BookingPaymentStatus.FullyPaid)
                : db.ChangeTracker.Entries<CheckIn>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("Injected test failure before final save.");
            return ValueTask.FromResult(result);
        }
    }

    internal sealed class Fixture : IAsyncDisposable
    {
        private readonly string _connection = Environment.GetEnvironmentVariable("COURTGO_TEST_SQLSERVER")!;
        public Guid CustomerId { get; } = Guid.NewGuid();
        public Guid StaffId { get; } = Guid.NewGuid();
        public Guid CenterId { get; } = Guid.NewGuid();
        public Guid SportId { get; } = Guid.NewGuid();
        public Guid CourtId { get; } = Guid.NewGuid();
        public Guid BookingId { get; } = Guid.NewGuid();
        public string Qr { get; } = "SqlTest-" + Guid.NewGuid().ToString("N");
        public TimeProvider Clock { get; private set; } = TimeProvider.System;

        public CourtGoDbContext Open(params IInterceptor[] interceptors) => new(
            new DbContextOptionsBuilder<CourtGoDbContext>().UseSqlServer(_connection)
                .AddInterceptors(interceptors).Options);
        public StaffBookingService Staff(CourtGoDbContext db) => new(db, new BookingCommandExecutor(db), TimeProvider.System);

        public static async Task<Fixture> CreateAsync(bool held = false, TimeProvider? clock = null)
        {
            var fixture = new Fixture { Clock = clock ?? TimeProvider.System };
            try { await fixture.SeedAsync(held); return fixture; }
            catch { await fixture.DisposeAsync(); throw; }
        }

        private async Task SeedAsync(bool held)
        {
            await using var db = Open();
            db.Users.AddRange(
                new User { Id = CustomerId, FullName = "Workflow SQL test", PhoneNumber = CustomerId.ToString("N")[..20],
                    PasswordHash = "TEST-NOT-A-LOGIN", Role = UserRole.Customer },
                new User { Id = StaffId, FullName = "Workflow SQL staff", PhoneNumber = StaffId.ToString("N")[..20],
                    PasswordHash = "TEST-NOT-A-LOGIN", Role = UserRole.Staff });
            db.SportCenters.Add(new SportCenter { Id = CenterId, Name = "Workflow test", AddressLine = "Test",
                District = "Test", City = "Test" });
            db.Sports.Add(new Sport { Id = SportId, Name = "Workflow test", Code = SportId.ToString("N")[..20] });
            db.Courts.Add(new Court { Id = CourtId, SportId = SportId, SportCenterId = CenterId,
                Code = "TEST", Name = "Renamed court", BasePricePerHour = 100000 });
            db.StaffAssignments.Add(new StaffAssignment { StaffUserId = StaffId, SportCenterId = CenterId });
            var start = Clock.GetUtcNow().AddDays(30);
            DateTimeOffset? expires = held ? Clock.GetUtcNow().AddMinutes(10) : null;
            db.Bookings.Add(new Booking
            {
                Id = BookingId, BookingCode = "TEST-" + BookingId.ToString("N")[..20],
                CourtId = CourtId, CreatedByUserId = CustomerId, CustomerUserId = CustomerId,
                CustomerNameSnapshot = "Snapshot customer", CustomerPhoneSnapshot = "0901111111",
                CourtNameSnapshot = "Snapshot court", CenterNameSnapshot = "Snapshot center", SportNameSnapshot = "Snapshot sport",
                StartAt = start, EndAt = start.AddHours(2), DurationMinutes = 120,
                TotalAmount = 200000, DepositAmount = 60000, DepositPercentSnapshot = 30,
                BookingStatus = held ? BookingStatus.PendingPayment : BookingStatus.Confirmed,
                PaymentStatus = held ? BookingPaymentStatus.Unpaid : BookingPaymentStatus.DepositPaid,
                QrToken = held ? null : Qr, HoldExpiresAt = expires
            });
            if (!held)
            db.Payments.Add(new Payment { BookingId = BookingId, PaymentKind = PaymentKind.Deposit,
                PaymentMethod = PaymentMethod.BankTransfer, Amount = 60000,
                TransactionStatus = PaymentTransactionStatus.Succeeded, PaidByUserId = CustomerId, PaidAt = DateTimeOffset.UtcNow });
            db.BookingSlots.Add(new BookingSlot { BookingId = BookingId, CourtId = CourtId,
                StartAt = start, EndAt = start.AddHours(1), UnitPrice = 100000,
                ReservationState = held ? ReservationState.Held : ReservationState.Reserved,
                HoldExpiresAt = expires, IsOccupying = true });
            if (held)
                db.BookingSlots.Add(new BookingSlot { BookingId = BookingId, CourtId = CourtId,
                    StartAt = start.AddHours(1), EndAt = start.AddHours(2), UnitPrice = 100000,
                    ReservationState = ReservationState.Held, HoldExpiresAt = expires, IsOccupying = true });
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await using var db = Open();
            await using var tx = await db.Database.BeginTransactionAsync();
            // Delete only this fixture's random IDs, in foreign-key order.
            await db.Notifications.Where(n => n.UserId == CustomerId).ExecuteDeleteAsync();
            await db.BookingStatusHistories.Where(h => h.BookingId == BookingId).ExecuteDeleteAsync();
            await db.CheckIns.Where(c => c.BookingId == BookingId).ExecuteDeleteAsync();
            await db.Payments.Where(p => p.BookingId == BookingId).ExecuteDeleteAsync();
            await db.BookingSlots.Where(s => s.BookingId == BookingId).ExecuteDeleteAsync();
            await db.Bookings.Where(b => b.Id == BookingId).ExecuteDeleteAsync();
            await db.StaffAssignments.Where(a => a.StaffUserId == StaffId).ExecuteDeleteAsync();
            await db.Courts.Where(c => c.Id == CourtId).ExecuteDeleteAsync();
            await db.Sports.Where(s => s.Id == SportId).ExecuteDeleteAsync();
            await db.SportCenters.Where(s => s.Id == CenterId).ExecuteDeleteAsync();
            await db.Users.Where(u => u.Id == CustomerId || u.Id == StaffId).ExecuteDeleteAsync();
            await tx.CommitAsync();
        }
    }

    [SqlServerFact]
    public async Task SqlQueries_PageInDatabase_AndConcurrentWorkflowCreatesOneOfEach()
    {
        await using var fixture = await Fixture.CreateAsync();
        var capture = new SqlCapture();
        await using (var db = fixture.Open(capture))
        {
            var query = new BookingQueryService(db, TimeProvider.System);
            var page = await query.GetMyBookingsAsync(fixture.CustomerId, new("upcoming", 1, 1));
            Assert.Single(page.Items);
            Assert.Equal(140000, page.Items[0].RemainingAmount);
            Assert.Contains(capture.Commands, c => c.Contains("OFFSET") && c.Contains("FETCH NEXT"));
            Assert.Equal(2, capture.Commands.Count); // COUNT + page with correlated aggregate, no N+1.
            var detail = await query.GetBookingDetailAsync(fixture.CustomerId, fixture.BookingId);
            Assert.Equal("Snapshot court", detail.Court.Name);
            Assert.Single(detail.Slots);
            var error = await Assert.ThrowsAsync<NotFoundException>(() => fixture.Staff(db).VerifyQrAsync(
                fixture.StaffId, new(fixture.Qr.ToUpperInvariant())));
            Assert.Equal(ErrorCodes.QrInvalid, error.Code);
        }

        async Task<bool> CollectAsync()
        {
            await using var db = fixture.Open();
            try
            {
                var result = await fixture.Staff(db).CollectRemainingPaymentAsync(fixture.StaffId, fixture.BookingId, new("Cash"));
                Assert.Equal(140000, result.AmountCollected);
                return true;
            }
            catch (ConflictException e) when (e.Code == ErrorCodes.PaymentAlreadyFullyPaid) { return false; }
        }
        var payments = await Task.WhenAll(CollectAsync(), CollectAsync());
        Assert.Single(payments.Where(p => p));

        async Task<CheckInResponse> CheckInAsync()
        {
            await using var db = fixture.Open();
            return await fixture.Staff(db).CheckInAsync(fixture.StaffId, fixture.BookingId, new(fixture.Qr));
        }
        var checkIns = await Task.WhenAll(CheckInAsync(), CheckInAsync());
        Assert.Equal(checkIns[0], checkIns[1]);
        await using var check = fixture.Open();
        Assert.Equal(1, await check.CheckIns.CountAsync(c => c.BookingId == fixture.BookingId));
        Assert.Equal(1, await check.BookingStatusHistories.CountAsync(c => c.BookingId == fixture.BookingId));
        Assert.Equal(1, await check.Notifications.CountAsync(c => c.ReferenceId == fixture.BookingId));
        Assert.Equal(1, await check.Payments.CountAsync(c => c.BookingId == fixture.BookingId && c.PaymentKind == PaymentKind.Remaining));
        Assert.Equal(BookingStatus.CheckedIn, (await check.Bookings.SingleAsync(b => b.Id == fixture.BookingId)).BookingStatus);
    }

    [SqlServerFact]
    public async Task SqlTransactions_RollBackPaymentAndCheckInOnSaveFailure()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var failing = fixture.Open(new FailSave(payment: true)))
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Staff(failing).CollectRemainingPaymentAsync(
                fixture.StaffId, fixture.BookingId, new("Cash")));
        await using (var check = fixture.Open())
        {
            Assert.Equal(1, await check.Payments.CountAsync(p => p.BookingId == fixture.BookingId));
            Assert.Equal(BookingPaymentStatus.DepositPaid,
                (await check.Bookings.SingleAsync(b => b.Id == fixture.BookingId)).PaymentStatus);
            await fixture.Staff(check).CollectRemainingPaymentAsync(fixture.StaffId, fixture.BookingId, new("Cash"));
        }
        await using (var failing = fixture.Open(new FailSave(payment: false)))
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Staff(failing).CheckInAsync(
                fixture.StaffId, fixture.BookingId, new(fixture.Qr)));
        await using var final = fixture.Open();
        Assert.Equal(BookingStatus.Confirmed, (await final.Bookings.SingleAsync(b => b.Id == fixture.BookingId)).BookingStatus);
        Assert.False(await final.CheckIns.AnyAsync(c => c.BookingId == fixture.BookingId));
        Assert.False(await final.BookingStatusHistories.AnyAsync(c => c.BookingId == fixture.BookingId));
        Assert.False(await final.Notifications.AnyAsync(c => c.ReferenceId == fixture.BookingId));
    }

    [SqlServerFact]
    public async Task SqlLock_WaitsForIndependentConnection_ThenReloadsCommittedState()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using var external = fixture.Open();
        await using var tx = await external.Database.BeginTransactionAsync();
        await external.Bookings.FromSqlInterpolated(
            $"SELECT * FROM [Bookings] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {fixture.BookingId}").SingleAsync();
        external.Payments.Add(new Payment { BookingId = fixture.BookingId, PaymentKind = PaymentKind.Remaining,
            PaymentMethod = PaymentMethod.Cash, Amount = 140000, TransactionStatus = PaymentTransactionStatus.Succeeded,
            PaidAt = DateTimeOffset.UtcNow, ConfirmedByUserId = fixture.StaffId });
        await external.SaveChangesAsync();
        var capture = new SqlCapture();
        await using var contender = fixture.Open(capture);
        var request = fixture.Staff(contender).CollectRemainingPaymentAsync(fixture.StaffId, fixture.BookingId, new("Cash"));
        try
        {
            await capture.LockQueryStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            // This lock belongs to a separate SQL connection outside the in-process command gate.
            Assert.NotSame(request, await Task.WhenAny(request, Task.Delay(200)));
        }
        finally { await tx.CommitAsync(); }
        var error = await Assert.ThrowsAsync<ConflictException>(() => request);
        Assert.Equal(ErrorCodes.PaymentAlreadyFullyPaid, error.Code);
        Assert.Equal(1, await contender.Payments.CountAsync(p => p.BookingId == fixture.BookingId && p.PaymentKind == PaymentKind.Remaining));
    }
}
