using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using CourtGo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CourtGo.UnitTests;

public class BookingHoldServiceTests
{
    private class SimpleExpiredHoldService : IExpiredBookingHoldService
    {
        private readonly CourtGoDbContext _db;
        private readonly TimeProvider _time;

        public SimpleExpiredHoldService(CourtGoDbContext db, TimeProvider time)
        {
            _db = db;
            _time = time;
        }

        public async Task ReleaseExpiredHoldsAsync(CancellationToken ct = default)
        {
            var nowUtc = _time.GetUtcNow();
            var expiredSlots = await _db.BookingSlots
                .Where(s => s.ReservationState == ReservationState.Held && s.IsOccupying && s.HoldExpiresAt != null && s.HoldExpiresAt <= nowUtc)
                .ToListAsync(ct);

            foreach (var s in expiredSlots)
            {
                s.ReservationState = ReservationState.Released;
                s.IsOccupying = false;
            }

            var expiredBookings = await _db.Bookings
                .Where(b => b.BookingStatus == BookingStatus.PendingPayment && b.HoldExpiresAt != null && b.HoldExpiresAt <= nowUtc)
                .ToListAsync(ct);

            foreach (var b in expiredBookings)
            {
                b.BookingStatus = BookingStatus.Expired;
            }

            if (expiredSlots.Count > 0 || expiredBookings.Count > 0)
            {
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    private static (CourtGoDbContext Db, FakeTimeProvider Time, BookingHoldService Service, User Customer, Court Court)
        CreateHarness(DateTimeOffset? fixedTime = null)
    {
        var options = new DbContextOptionsBuilder<CourtGoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new CourtGoDbContext(options);
        var time = new FakeTimeProvider();
        // Default: 2026-10-10 10:00:00 UTC (17:00:00 VN)
        time.Now = fixedTime ?? new DateTimeOffset(2026, 10, 10, 10, 0, 0, TimeSpan.Zero);

        var expiredService = new SimpleExpiredHoldService(db, time);
        var codeGen = new BookingCodeGenerator();
        var service = new BookingHoldService(db, expiredService, codeGen, time, NullLogger<BookingHoldService>.Instance);

        // Seed base setup
        var customer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nguyen Van A",
            PhoneNumber = "0901234567",
            Email = "customer@courtgo.vn",
            Role = UserRole.Customer,
            IsActive = true
        };
        db.Users.Add(customer);

        var sport = new Sport
        {
            Id = Guid.NewGuid(),
            Code = "badminton",
            Name = "Cầu lông",
            IsActive = true
        };
        db.Sports.Add(sport);

        var center = new SportCenter
        {
            Id = Guid.NewGuid(),
            Name = "CourtGo Q7",
            AddressLine = "123 Nguyen Thi Thap",
            District = "District 7",
            City = "Ho Chi Minh",
            TimeZoneId = "Asia/Ho_Chi_Minh",
            Status = SportCenterStatus.Active
        };
        db.SportCenters.Add(center);

        var court = new Court
        {
            Id = Guid.NewGuid(),
            Code = "A1",
            Name = "Sân A1",
            SportId = sport.Id,
            Sport = sport,
            SportCenterId = center.Id,
            SportCenter = center,
            BasePricePerHour = 100000m,
            Status = CourtStatus.Active
        };
        db.Courts.Add(court);

        // Default settings
        db.SystemSettings.Add(new SystemSetting
        {
            Id = 1,
            HoldDurationMinutes = 10,
            MinBookingLeadMinutes = 30,
            DefaultDepositPercent = 30.00m
        });

        // Operating hours: 06:00 to 22:00 every day
        foreach (CourtGoDayOfWeek day in Enum.GetValues<CourtGoDayOfWeek>())
        {
            db.OperatingHours.Add(new OperatingHour
            {
                Id = Guid.NewGuid(),
                SportCenterId = center.Id,
                DayOfWeek = day,
                OpenTime = new TimeOnly(6, 0),
                CloseTime = new TimeOnly(22, 0),
                IsClosed = false
            });
        }

        db.SaveChanges();

        return (db, time, service, customer, court);
    }

    [Fact]
    public async Task HoldAsync_EmptySlots_ThrowsValidationException_InvalidSlotSelection()
    {
        var (_, _, service, customer, court) = CreateHarness();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset>())));

        Assert.Equal(ErrorCodes.InvalidSlotSelection, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_DuplicateSlots_ThrowsValidationException_InvalidSlotSelection()
    {
        var (_, _, service, customer, court) = CreateHarness();
        var slot = new DateTimeOffset(2026, 10, 10, 19, 0, 0, TimeSpan.FromHours(7));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { slot, slot })));

        Assert.Equal(ErrorCodes.InvalidSlotSelection, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_NotAlignedToHour_ThrowsValidationException()
    {
        var (_, _, service, customer, court) = CreateHarness();
        var slot = new DateTimeOffset(2026, 10, 10, 19, 30, 0, TimeSpan.FromHours(7));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { slot })));

        Assert.Equal(ErrorCodes.InvalidSlotSelection, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_NonConsecutiveSlots_ThrowsValidationException()
    {
        var (_, _, service, customer, court) = CreateHarness();
        var s1 = new DateTimeOffset(2026, 10, 10, 18, 0, 0, TimeSpan.FromHours(7));
        var s2 = new DateTimeOffset(2026, 10, 10, 20, 0, 0, TimeSpan.FromHours(7));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { s1, s2 })));

        Assert.Equal(ErrorCodes.InvalidSlotSelection, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_CrossCalendarDays_ThrowsValidationException()
    {
        var (_, _, service, customer, court) = CreateHarness();
        var s1 = new DateTimeOffset(2026, 10, 10, 23, 0, 0, TimeSpan.FromHours(7));
        var s2 = new DateTimeOffset(2026, 10, 11, 0, 0, 0, TimeSpan.FromHours(7));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { s1, s2 })));

        Assert.Equal(ErrorCodes.InvalidSlotSelection, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_InactiveCustomer_ThrowsForbiddenException()
    {
        var (db, _, service, customer, court) = CreateHarness();
        customer.IsActive = false;
        await db.SaveChangesAsync();

        var s1 = new DateTimeOffset(2026, 10, 10, 19, 0, 0, TimeSpan.FromHours(7));
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { s1 })));

        Assert.Equal(ErrorCodes.AccountInactive, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_StaffRole_ThrowsForbiddenException()
    {
        var (db, _, service, customer, court) = CreateHarness();
        customer.Role = UserRole.Staff;
        await db.SaveChangesAsync();

        var s1 = new DateTimeOffset(2026, 10, 10, 19, 0, 0, TimeSpan.FromHours(7));
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { s1 })));

        Assert.Equal(ErrorCodes.Forbidden, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_CourtNotFound_ThrowsNotFoundException()
    {
        var (_, _, service, customer, _) = CreateHarness();
        var nonExistentCourtId = Guid.NewGuid();

        var s1 = new DateTimeOffset(2026, 10, 10, 19, 0, 0, TimeSpan.FromHours(7));
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(nonExistentCourtId, new List<DateTimeOffset> { s1 })));

        Assert.Equal(ErrorCodes.CourtNotFound, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_CourtInactive_ThrowsConflictException()
    {
        var (db, _, service, customer, court) = CreateHarness();
        court.Status = CourtStatus.Inactive;
        await db.SaveChangesAsync();

        var s1 = new DateTimeOffset(2026, 10, 10, 19, 0, 0, TimeSpan.FromHours(7));
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { s1 })));

        Assert.Equal(ErrorCodes.CourtNotBookable, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_CenterClosedOnExceptionDate_ThrowsConflictException()
    {
        var (db, _, service, customer, court) = CreateHarness();
        db.OperatingHourExceptions.Add(new OperatingHourException
        {
            Id = Guid.NewGuid(),
            SportCenterId = court.SportCenterId,
            Date = new DateOnly(2026, 10, 10),
            IsClosed = true
        });
        await db.SaveChangesAsync();

        var s1 = new DateTimeOffset(2026, 10, 10, 19, 0, 0, TimeSpan.FromHours(7));
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { s1 })));

        Assert.Equal(ErrorCodes.CourtNotBookable, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_OutsideOperatingHours_ThrowsValidationException()
    {
        var (_, _, service, customer, court) = CreateHarness();
        // Center closes at 22:00, 22:00-23:00 is outside
        var s1 = new DateTimeOffset(2026, 10, 10, 22, 0, 0, TimeSpan.FromHours(7));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { s1 })));

        Assert.Equal(ErrorCodes.InvalidSlotSelection, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_ViolatesLeadTime_ThrowsConflictException()
    {
        // Current VN time is 17:00, min lead time = 30m -> earliest allowed is 17:30. Slot at 17:00 should fail.
        var (_, _, service, customer, court) = CreateHarness();
        var s1 = new DateTimeOffset(2026, 10, 10, 17, 0, 0, TimeSpan.FromHours(7));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { s1 })));

        Assert.Equal(ErrorCodes.BookingSlotConflict, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_CourtBlockOverlap_ThrowsConflictException()
    {
        var (db, _, service, customer, court) = CreateHarness();
        var s1 = new DateTimeOffset(2026, 10, 10, 19, 0, 0, TimeSpan.FromHours(7));

        db.CourtBlocks.Add(new CourtBlock
        {
            Id = Guid.NewGuid(),
            CourtId = court.Id,
            StartAt = s1,
            EndAt = s1.AddHours(1),
            Type = CourtBlockType.Maintenance,
            Reason = "Lau sàn"
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { s1 })));

        Assert.Equal(ErrorCodes.BookingSlotConflict, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_ActiveHeldSlotOverlap_ThrowsConflictException()
    {
        var (db, time, service, customer, court) = CreateHarness();
        var s1 = new DateTimeOffset(2026, 10, 10, 19, 0, 0, TimeSpan.FromHours(7));

        db.BookingSlots.Add(new BookingSlot
        {
            Id = Guid.NewGuid(),
            BookingId = Guid.NewGuid(),
            CourtId = court.Id,
            StartAt = s1,
            EndAt = s1.AddHours(1),
            ReservationState = ReservationState.Held,
            IsOccupying = true,
            HoldExpiresAt = time.GetUtcNow().AddMinutes(5),
            UnitPrice = 100000m
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { s1 })));

        Assert.Equal(ErrorCodes.BookingSlotConflict, ex.Code);
    }

    [Fact]
    public async Task HoldAsync_ExpiredHold_ReleasedAndSucceeds()
    {
        var (db, time, service, customer, court) = CreateHarness();
        var s1 = new DateTimeOffset(2026, 10, 10, 19, 0, 0, TimeSpan.FromHours(7));

        // Expired hold: expired 2 minutes ago
        var expiredBookingId = Guid.NewGuid();
        db.Bookings.Add(new Booking
        {
            Id = expiredBookingId,
            BookingCode = "OLD-01",
            CustomerUserId = customer.Id,
            CourtId = court.Id,
            StartAt = s1,
            EndAt = s1.AddHours(1),
            DurationMinutes = 60,
            BookingStatus = BookingStatus.PendingPayment,
            HoldExpiresAt = time.GetUtcNow().AddMinutes(-2),
            CreatedByUserId = customer.Id
        });

        db.BookingSlots.Add(new BookingSlot
        {
            Id = Guid.NewGuid(),
            BookingId = expiredBookingId,
            CourtId = court.Id,
            StartAt = s1,
            EndAt = s1.AddHours(1),
            ReservationState = ReservationState.Held,
            IsOccupying = true,
            HoldExpiresAt = time.GetUtcNow().AddMinutes(-2),
            UnitPrice = 100000m
        });
        await db.SaveChangesAsync();

        var result = await service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { s1 }));

        Assert.NotNull(result);
        Assert.Equal("PendingPayment", result.BookingStatus);
        Assert.Single(result.Slots);
    }

    [Fact]
    public async Task HoldAsync_MultiSlot_CalculatesPricesAndDepositCorrectly()
    {
        var (db, _, service, customer, court) = CreateHarness();
        // 2026-10-10 is Saturday
        var s1 = new DateTimeOffset(2026, 10, 10, 19, 0, 0, TimeSpan.FromHours(7));
        var s2 = new DateTimeOffset(2026, 10, 10, 20, 0, 0, TimeSpan.FromHours(7));

        // Add PriceRule for 19:00 - 20:00: 120,000 VND
        db.PriceRules.Add(new PriceRule
        {
            Id = Guid.NewGuid(),
            CourtId = court.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            StartTime = new TimeOnly(19, 0),
            EndTime = new TimeOnly(20, 0),
            PricePerHour = 120000m,
            IsActive = true
        });
        // 20:00 - 21:00 has no price rule, falls back to court.BasePricePerHour = 100,000 VND
        await db.SaveChangesAsync();

        var result = await service.HoldAsync(customer.Id, new BookingHoldRequest(court.Id, new List<DateTimeOffset> { s1, s2 }));

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(120000m, result.Slots[0].Price);
        Assert.Equal(100000m, result.Slots[1].Price);
        Assert.Equal(220000m, result.TotalAmount);
        Assert.Equal(30m, result.DepositPercent);
        // Deposit = 220000 * 30% = 66,000
        Assert.Equal(66000m, result.DepositAmount);
        // Remaining = 220000 - 66000 = 154,000
        Assert.Equal(154000m, result.RemainingAmount);
        Assert.Equal(120, result.DurationMinutes);
        Assert.Equal(s1, result.StartAt);
        Assert.Equal(s2.AddHours(1), result.EndAt);

        // Verify Snapshots
        var createdBooking = await db.Bookings
            .Include(b => b.Slots)
            .Include(b => b.StatusHistories)
            .FirstAsync(b => b.Id == result.BookingId);

        Assert.Equal(customer.FullName, createdBooking.CustomerNameSnapshot);
        Assert.Equal(customer.PhoneNumber, createdBooking.CustomerPhoneSnapshot);
        Assert.Equal(customer.Email, createdBooking.CustomerEmailSnapshot);
        Assert.Equal(court.Name, createdBooking.CourtNameSnapshot);
        Assert.Equal(court.SportCenter!.Name, createdBooking.CenterNameSnapshot);
        Assert.Equal(court.Sport!.Name, createdBooking.SportNameSnapshot);
        Assert.Equal(BookingStatus.PendingPayment, createdBooking.BookingStatus);
        Assert.Equal(BookingPaymentStatus.Unpaid, createdBooking.PaymentStatus);
        Assert.Null(createdBooking.QrToken);
        Assert.Single(createdBooking.StatusHistories);
        Assert.Equal(BookingStatus.PendingPayment, createdBooking.StatusHistories.First().ToStatus);
    }
}
