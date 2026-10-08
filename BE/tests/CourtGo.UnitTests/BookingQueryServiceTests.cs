using System.Text.Json;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using CourtGo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.UnitTests;

public class BookingQueryServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private static CourtGoDbContext CreateDb() => new(new DbContextOptionsBuilder<CourtGoDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Booking MakeBooking(Guid customerId, BookingStatus status, int days = 1) => new()
    {
        CustomerUserId = customerId, BookingCode = Guid.NewGuid().ToString("N")[..20],
        BookingStatus = status, PaymentStatus = BookingPaymentStatus.DepositPaid,
        CustomerNameSnapshot = "Original customer", CustomerPhoneSnapshot = "0901111111",
        CourtNameSnapshot = "Original court", CenterNameSnapshot = "Original center", SportNameSnapshot = "Original sport",
        CourtId = Guid.NewGuid(), StartAt = Now.AddDays(days), EndAt = Now.AddDays(days).AddHours(2),
        DurationMinutes = 120, TotalAmount = 200000, DepositAmount = 60000,
        QrToken = "Secret-" + Guid.NewGuid().ToString("N"), HoldExpiresAt = Now.AddMinutes(5)
    };

    [Fact]
    public async Task Upcoming_FiltersOwnershipAndExpiredHolds_SortsAndPages()
    {
        await using var db = CreateDb();
        var customer = Guid.NewGuid();
        var bookings = new[]
        {
            MakeBooking(customer, BookingStatus.Confirmed, 4),
            MakeBooking(customer, BookingStatus.CheckedIn, 3),
            MakeBooking(customer, BookingStatus.InProgress, 2),
            MakeBooking(customer, BookingStatus.PendingPayment, 1),
            MakeBooking(Guid.NewGuid(), BookingStatus.Confirmed, 0),
            MakeBooking(customer, BookingStatus.Completed, 0),
            MakeBooking(customer, BookingStatus.PendingPayment, 0),
            MakeBooking(customer, BookingStatus.PendingPayment, 0)
        };
        bookings[6].HoldExpiresAt = Now;
        bookings[7].HoldExpiresAt = null;
        db.Bookings.AddRange(bookings);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new BookingQueryService(db, new Clock());
        var first = await service.GetMyBookingsAsync(customer, new("UPCOMING", 1, 2));
        var second = await service.GetMyBookingsAsync(customer, new("upcoming", 2, 2));
        Assert.Equal(4, first.TotalItems);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal(new[] { bookings[3].Id, bookings[2].Id }, first.Items.Select(i => i.BookingId));
        Assert.Equal(new[] { bookings[1].Id, bookings[0].Id }, second.Items.Select(i => i.BookingId));
        Assert.Empty((await service.GetMyBookingsAsync(customer, new("upcoming", 3, 2))).Items);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("cancelled")]
    public async Task PastGroups_UseCorrectStates_AndDescendingSort(string group)
    {
        await using var db = CreateDb();
        var customer = Guid.NewGuid();
        var states = group == "completed" ? new[] { BookingStatus.Completed }
            : new[] { BookingStatus.Cancelled, BookingStatus.Expired, BookingStatus.NoShow };
        var expected = states.SelectMany(s => new[] { MakeBooking(customer, s, -2), MakeBooking(customer, s, -1) }).ToList();
        db.Bookings.AddRange(expected);
        db.Bookings.Add(MakeBooking(customer, BookingStatus.Confirmed));
        db.Bookings.Add(MakeBooking(Guid.NewGuid(), states[0]));
        await db.SaveChangesAsync();
        var result = await new BookingQueryService(db, new Clock()).GetMyBookingsAsync(customer, new(group));
        Assert.Equal(expected.Count, result.TotalItems);
        Assert.All(result.Items, b => Assert.Contains(Enum.Parse<BookingStatus>(b.BookingStatus), states));
        Assert.Equal(result.Items.Select(i => i.StartAt).OrderByDescending(t => t), result.Items.Select(i => i.StartAt));
    }

    [Theory]
    [InlineData("invalid", 1, 20, ErrorCodes.InvalidBookingStatusGroup)]
    [InlineData("", 1, 20, ErrorCodes.InvalidBookingStatusGroup)]
    [InlineData("upcoming", 0, 20, ErrorCodes.ValidationError)]
    [InlineData("upcoming", 1, 101, ErrorCodes.ValidationError)]
    [InlineData("upcoming", 1, 0, ErrorCodes.ValidationError)]
    [InlineData("upcoming", int.MaxValue, 100, ErrorCodes.ValidationError)]
    public async Task InvalidQueries_AreRejected(string group, int page, int size, string code)
    {
        await using var db = CreateDb();
        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            new BookingQueryService(db, new Clock()).GetMyBookingsAsync(Guid.NewGuid(), new(group, page, size)));
        Assert.Equal(code, error.Code);
    }

    [Fact]
    public async Task DetailAndList_UseSnapshotsAndSuccessfulCharges_WithoutQrLeak()
    {
        await using var db = CreateDb();
        var customer = Guid.NewGuid();
        var booking = MakeBooking(customer, BookingStatus.Confirmed);
        booking.Court = new Court { Id = booking.CourtId, Name = "Renamed court" };
        booking.Payments = new[]
        {
            new Payment { PaymentKind = PaymentKind.Deposit, TransactionStatus = PaymentTransactionStatus.Succeeded, Amount = 60000 },
            new Payment { PaymentKind = PaymentKind.Remaining, TransactionStatus = PaymentTransactionStatus.Succeeded, Amount = 20000 },
            new Payment { PaymentKind = PaymentKind.Refund, TransactionStatus = PaymentTransactionStatus.Succeeded, Amount = 5000 },
            new Payment { PaymentKind = PaymentKind.Remaining, TransactionStatus = PaymentTransactionStatus.Failed, Amount = 200000 },
            new Payment { PaymentKind = PaymentKind.Deposit, TransactionStatus = PaymentTransactionStatus.Pending, Amount = 200000 }
        };
        booking.Slots = new[]
        {
            new BookingSlot { StartAt = booking.StartAt.AddHours(1), EndAt = booking.EndAt, UnitPrice = 100000 },
            new BookingSlot { StartAt = booking.StartAt, EndAt = booking.StartAt.AddHours(1), UnitPrice = 100000 }
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new BookingQueryService(db, new Clock());
        var detail = await service.GetBookingDetailAsync(customer, booking.Id);
        Assert.Equal("Original customer", detail.Customer.FullName);
        Assert.Equal("Original court", detail.Court.Name);
        Assert.Equal("Original center", detail.Center.Name);
        Assert.Equal("Original sport", detail.Sport.Name);
        Assert.Equal(80000, detail.Payment.PaidAmount);
        Assert.Equal(120000, detail.Payment.RemainingAmount);
        Assert.Equal(booking.StartAt, detail.Slots[0].StartAt);
        var list = await service.GetMyBookingsAsync(customer, new());
        Assert.Equal(80000, list.Items.Single().PaidAmount);
        Assert.True(list.Items.Single().HasQr);
        Assert.DoesNotContain(booking.QrToken!, JsonSerializer.Serialize(list));
        Assert.DoesNotContain("QrToken", JsonSerializer.Serialize(detail));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(BookingStatus.PendingPayment)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Expired)]
    [InlineData(BookingStatus.NoShow)]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.InProgress)]
    [InlineData(BookingStatus.Completed)]
    public async Task Qr_OnlyAvailableWhileConfirmed(BookingStatus state)
    {
        await using var db = CreateDb();
        var customer = Guid.NewGuid();
        var booking = MakeBooking(customer, state);
        db.Add(booking);
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            new BookingQueryService(db, new Clock()).GetQrAsync(customer, booking.Id));
        Assert.Equal(ErrorCodes.QrNotAvailable, error.Code);
    }

    [Fact]
    public async Task QrAndDetail_EnforceOwnership_AndMissingTokenIsUnavailable()
    {
        await using var db = CreateDb();
        var customer = Guid.NewGuid();
        var booking = MakeBooking(customer, BookingStatus.Confirmed);
        db.Add(booking);
        await db.SaveChangesAsync();
        var service = new BookingQueryService(db, new Clock());
        Assert.Equal(booking.QrToken, (await service.GetQrAsync(customer, booking.Id)).QrValue);
        foreach (var id in new[] { Guid.NewGuid(), booking.Id })
        {
            var detailError = await Assert.ThrowsAsync<NotFoundException>(() => service.GetBookingDetailAsync(Guid.NewGuid(), id));
            var qrError = await Assert.ThrowsAsync<NotFoundException>(() => service.GetQrAsync(Guid.NewGuid(), id));
            Assert.Equal(ErrorCodes.BookingNotFound, detailError.Code);
            Assert.Equal(ErrorCodes.BookingNotFound, qrError.Code);
        }
        booking.QrToken = null;
        await db.SaveChangesAsync();
        Assert.Equal(ErrorCodes.QrNotAvailable,
            (await Assert.ThrowsAsync<ConflictException>(() => service.GetQrAsync(customer, booking.Id))).Code);
    }

    [Fact]
    public async Task PaymentSummary_ClampsOverpayment_AndHandlesNoTransactions()
    {
        await using var db = CreateDb();
        var booking = MakeBooking(Guid.NewGuid(), BookingStatus.Confirmed);
        db.Add(booking);
        await db.SaveChangesAsync();
        var service = new BookingQueryService(db, new Clock());
        Assert.Equal(200000, (await service.GetBookingDetailAsync(booking.CustomerUserId!.Value, booking.Id)).Payment.RemainingAmount);
        db.Payments.Add(new Payment { BookingId = booking.Id, Amount = 250000, PaymentKind = PaymentKind.Deposit,
            TransactionStatus = PaymentTransactionStatus.Succeeded });
        await db.SaveChangesAsync();
        Assert.Equal(0, (await service.GetBookingDetailAsync(booking.CustomerUserId.Value, booking.Id)).Payment.RemainingAmount);
    }
}
