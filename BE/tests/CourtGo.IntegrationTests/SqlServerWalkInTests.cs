using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
namespace CourtGo.IntegrationTests;
[Collection("SqlServer workflow")]
public class SqlServerWalkInTests
{
    private sealed class TestClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
    private sealed class TestCode(string code) : IBookingCodeGenerator
    { public string GenerateBookingCode(DateOnly date) => code; }

    [SqlServerFact]
    public async Task ConcurrentWalkIns_OnlyOneReservationAndPayment_StaffQueriesTranslate()
    {
        var clock = new TestClock();
        await using var fixture = await SqlServerBookingWorkflowTests.Fixture.CreateAsync(clock: clock);
        var code = "WTEST-" + Guid.NewGuid().ToString("N")[..20];
        var hours = Enumerable.Range(1, 7).Select(day => new OperatingHour { Id = Guid.NewGuid(),
            SportCenterId = fixture.CenterId, DayOfWeek = (CourtGoDayOfWeek)day,
            OpenTime = new(6, 0), CloseTime = new(22, 0) }).ToList();
        var hourIds = hours.Select(h => h.Id).ToArray();
        try
        {
            await using (var db = fixture.Open())
            { db.OperatingHours.AddRange(hours); await db.SaveChangesAsync(); }
            var start = new DateTimeOffset(2000, 1, 3, 10, 0, 0, TimeSpan.FromHours(7));
            async Task<bool> CreateAsync()
            {
                await using var db = fixture.Open();
                var service = new BookingHoldService(db, new ExpiredBookingHoldService(db, new BookingCommandExecutor(db), clock),
                    new TestCode(code), clock, NullLogger<BookingHoldService>.Instance);
                try
                {
                    var result = await service.CreateWalkInAsync(fixture.StaffId, new("Guest", "0905555555", fixture.CourtId, new() { start }));
                    Assert.Equal("FullyPaid", result.Booking.PaymentStatus);
                    Assert.Equal(100000, result.Payment.PaidAmount);
                    return true;
                }
                catch (ConflictException e) when (e.Code == ErrorCodes.BookingSlotConflict) { return false; }
            }
            Assert.Single((await Task.WhenAll(CreateAsync(), CreateAsync())).Where(x => x));
            await using var check = fixture.Open();
            var booking = await check.Bookings.SingleAsync(b => b.BookingCode == code);
            Assert.Null(booking.CustomerUserId);
            Assert.Equal(1, await check.BookingSlots.CountAsync(s => s.BookingId == booking.Id && s.IsOccupying));
            Assert.Equal(1, await check.Payments.CountAsync(p => p.BookingId == booking.Id));
            var staff = new StaffOperationsService(check, clock);
            Assert.Equal(2, (await staff.GetBookingsAsync(fixture.StaffId, new())).TotalItems);
            Assert.Equal(2, (await staff.GetDashboardAsync(fixture.StaffId)).Upcoming);
        }
        finally
        {
            await using var cleanup = fixture.Open();
            await using var tx = await cleanup.Database.BeginTransactionAsync();
            // Only the booking with this test's random unique code, plus the seven explicit new hour IDs.
            var id = await cleanup.Bookings.Where(b => b.BookingCode == code && b.CreatedByUserId == fixture.StaffId)
                .Select(b => (Guid?)b.Id).SingleOrDefaultAsync();
            if (id is not null)
            {
                await cleanup.BookingStatusHistories.Where(h => h.BookingId == id).ExecuteDeleteAsync();
                await cleanup.Payments.Where(p => p.BookingId == id).ExecuteDeleteAsync();
                await cleanup.BookingSlots.Where(s => s.BookingId == id).ExecuteDeleteAsync();
                await cleanup.Bookings.Where(b => b.Id == id).ExecuteDeleteAsync();
            }
            await cleanup.OperatingHours.Where(h => hourIds.Contains(h.Id)).ExecuteDeleteAsync();
            await tx.CommitAsync();
        }
    }
}
