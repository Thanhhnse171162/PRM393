using CourtGo.Application.Availability;
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

public class AvailabilityServiceTests
{
    private class NoOpHoldService : IExpiredBookingHoldService
    {
        public Task ReleaseExpiredHoldsAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private static (CourtGoDbContext Db, FakeTimeProvider TimeProvider, AvailabilityService Service) CreateTestHarness(DateTimeOffset? utcNow = null)
    {
        var options = new DbContextOptionsBuilder<CourtGoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new CourtGoDbContext(options);
        var timeProvider = new FakeTimeProvider();
        if (utcNow.HasValue)
        {
            timeProvider.Now = utcNow.Value;
        }

        var holdService = new NoOpHoldService();
        var service = new AvailabilityService(db, holdService, timeProvider, NullLogger<AvailabilityService>.Instance);

        return (db, timeProvider, service);
    }

    private static (Sport Sport, SportCenter Center, Court Court) SeedBaseEntities(CourtGoDbContext db, string timeZoneId = "Asia/Ho_Chi_Minh")
    {
        var sport = new Sport
        {
            Id = Guid.NewGuid(),
            Code = "badminton",
            Name = "Cầu lông",
            IsActive = true
        };

        var center = new SportCenter
        {
            Id = Guid.NewGuid(),
            Name = "CourtGo Center",
            AddressLine = "123 Street",
            District = "District 1",
            City = "Ho Chi Minh",
            TimeZoneId = timeZoneId,
            Status = SportCenterStatus.Active
        };

        var court = new Court
        {
            Id = Guid.NewGuid(),
            SportCenterId = center.Id,
            SportCenter = center,
            SportId = sport.Id,
            Sport = sport,
            Code = "A1",
            Name = "Sân A1",
            BasePricePerHour = 100000m,
            Status = CourtStatus.Active
        };

        var settings = new SystemSetting
        {
            Id = 1,
            MinBookingLeadMinutes = 30
        };

        db.Sports.Add(sport);
        db.SportCenters.Add(center);
        db.Courts.Add(court);
        db.SystemSettings.Add(settings);
        db.SaveChanges();

        return (sport, center, court);
    }

    // 1. 17:00-22:00 generates exactly 5 one-hour slots
    [Fact]
    public async Task Generates_Exactly_5_OneHourSlots_For_17To22()
    {
        var (db, time, service) = CreateTestHarness(new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.FromHours(7)));
        var (_, center, court) = SeedBaseEntities(db);

        // Saturday = 6
        db.OperatingHours.Add(new OperatingHour
        {
            SportCenterId = center.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            OpenTime = new TimeOnly(17, 0),
            CloseTime = new TimeOnly(22, 0),
            IsClosed = false
        });
        await db.SaveChangesAsync();

        var date = new DateOnly(2026, 10, 10); // Saturday
        var result = await service.GetCourtAvailabilityAsync(court.Id, date);

        Assert.False(result.IsClosed);
        Assert.NotNull(result.OpeningHours);
        Assert.Equal("17:00", result.OpeningHours.OpenTime);
        Assert.Equal("22:00", result.OpeningHours.CloseTime);
        Assert.Equal(5, result.Slots.Count);
        Assert.Equal(new TimeOnly(17, 0), TimeOnly.FromTimeSpan(result.Slots[0].StartAt.TimeOfDay));
        Assert.Equal(new TimeOnly(18, 0), TimeOnly.FromTimeSpan(result.Slots[0].EndAt.TimeOfDay));
        Assert.Equal(new TimeOnly(21, 0), TimeOnly.FromTimeSpan(result.Slots[4].StartAt.TimeOfDay));
        Assert.Equal(new TimeOnly(22, 0), TimeOnly.FromTimeSpan(result.Slots[4].EndAt.TimeOfDay));
    }

    // 2. partial final hour is not generated (17:30-22:00 generates 4 full slots)
    [Fact]
    public async Task Partial_Final_Hour_Is_Not_Generated()
    {
        var (db, time, service) = CreateTestHarness(new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.FromHours(7)));
        var (_, center, court) = SeedBaseEntities(db);

        db.OperatingHours.Add(new OperatingHour
        {
            SportCenterId = center.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            OpenTime = new TimeOnly(17, 30),
            CloseTime = new TimeOnly(22, 0),
            IsClosed = false
        });
        await db.SaveChangesAsync();

        var date = new DateOnly(2026, 10, 10);
        var result = await service.GetCourtAvailabilityAsync(court.Id, date);

        Assert.Equal(4, result.Slots.Count);
        Assert.Equal(new TimeOnly(17, 30), TimeOnly.FromTimeSpan(result.Slots[0].StartAt.TimeOfDay));
        Assert.Equal(new TimeOnly(18, 30), TimeOnly.FromTimeSpan(result.Slots[0].EndAt.TimeOfDay));
        Assert.Equal(new TimeOnly(20, 30), TimeOnly.FromTimeSpan(result.Slots[3].StartAt.TimeOfDay));
        Assert.Equal(new TimeOnly(21, 30), TimeOnly.FromTimeSpan(result.Slots[3].EndAt.TimeOfDay));
    }

    // 3. OperatingHourException overrides weekly OperatingHours
    [Fact]
    public async Task OperatingHourException_Overrides_Weekly_OperatingHours()
    {
        var (db, time, service) = CreateTestHarness(new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.FromHours(7)));
        var (_, center, court) = SeedBaseEntities(db);

        // Weekly is 17:00-22:00 (5 slots)
        db.OperatingHours.Add(new OperatingHour
        {
            SportCenterId = center.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            OpenTime = new TimeOnly(17, 0),
            CloseTime = new TimeOnly(22, 0),
            IsClosed = false
        });

        // Exception for 2026-10-10 is 08:00-10:00 (2 slots)
        db.OperatingHourExceptions.Add(new OperatingHourException
        {
            SportCenterId = center.Id,
            Date = new DateOnly(2026, 10, 10),
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(10, 0),
            IsClosed = false
        });
        await db.SaveChangesAsync();

        var result = await service.GetCourtAvailabilityAsync(court.Id, new DateOnly(2026, 10, 10));

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal("08:00", result.OpeningHours!.OpenTime);
        Assert.Equal("10:00", result.OpeningHours!.CloseTime);
    }

    // 4. closed exception returns no slots
    [Fact]
    public async Task Closed_Exception_Returns_No_Slots()
    {
        var (db, time, service) = CreateTestHarness(new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.FromHours(7)));
        var (_, center, court) = SeedBaseEntities(db);

        db.OperatingHours.Add(new OperatingHour
        {
            SportCenterId = center.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            OpenTime = new TimeOnly(17, 0),
            CloseTime = new TimeOnly(22, 0),
            IsClosed = false
        });

        db.OperatingHourExceptions.Add(new OperatingHourException
        {
            SportCenterId = center.Id,
            Date = new DateOnly(2026, 10, 10),
            IsClosed = true
        });
        await db.SaveChangesAsync();

        var result = await service.GetCourtAvailabilityAsync(court.Id, new DateOnly(2026, 10, 10));

        Assert.True(result.IsClosed);
        Assert.Null(result.OpeningHours);
        Assert.Empty(result.Slots);
    }

    // 5. future date shows all valid opening slots
    [Fact]
    public async Task Future_Date_Shows_All_Valid_Opening_Slots()
    {
        // Current time is Saturday evening at 21:00
        var currentLocal = new DateTimeOffset(2026, 10, 10, 21, 0, 0, TimeSpan.FromHours(7));
        var (db, time, service) = CreateTestHarness(currentLocal.ToUniversalTime());
        var (_, center, court) = SeedBaseEntities(db);

        // Sunday operating hours 08:00 - 12:00
        db.OperatingHours.Add(new OperatingHour
        {
            SportCenterId = center.Id,
            DayOfWeek = CourtGoDayOfWeek.Sunday,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(12, 0),
            IsClosed = false
        });
        await db.SaveChangesAsync();

        var tomorrow = new DateOnly(2026, 10, 11);
        var result = await service.GetCourtAvailabilityAsync(court.Id, tomorrow);

        Assert.Equal(4, result.Slots.Count);
        Assert.Equal("08:00", result.OpeningHours!.OpenTime);
    }

    // 6 & 7. today's past slots and lead time filtering
    [Fact]
    public async Task Today_Past_Slots_And_LeadTime_Filtered_Out()
    {
        // Current local time: 19:10 on Saturday
        // MinBookingLeadMinutes: 30 minutes
        // Allowed slot start >= 19:40
        var currentLocal = new DateTimeOffset(2026, 10, 10, 19, 10, 0, TimeSpan.FromHours(7));
        var (db, time, service) = CreateTestHarness(currentLocal.ToUniversalTime());
        var (_, center, court) = SeedBaseEntities(db);

        db.OperatingHours.Add(new OperatingHour
        {
            SportCenterId = center.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            OpenTime = new TimeOnly(17, 0),
            CloseTime = new TimeOnly(22, 0),
            IsClosed = false
        });
        await db.SaveChangesAsync();

        var today = new DateOnly(2026, 10, 10);
        var result = await service.GetCourtAvailabilityAsync(court.Id, today);

        // Slots originally: 17-18, 18-19, 19-20, 20-21, 21-22
        // Filtered out:
        // 17-18 (past)
        // 18-19 (past)
        // 19-20 (start 19:00 < 19:40 lead time threshold)
        // Kept:
        // 20-21 (start 20:00 >= 19:40)
        // 21-22 (start 21:00 >= 19:40)
        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new TimeOnly(20, 0), TimeOnly.FromTimeSpan(result.Slots[0].StartAt.TimeOfDay));
        Assert.Equal(new TimeOnly(21, 0), TimeOnly.FromTimeSpan(result.Slots[1].StartAt.TimeOfDay));
    }

    // 8. inactive Court is not bookable
    [Fact]
    public async Task Inactive_Court_Throws_NotFoundException()
    {
        var (db, time, service) = CreateTestHarness(new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.FromHours(7)));
        var (_, _, court) = SeedBaseEntities(db);

        court.Status = CourtStatus.Inactive;
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetCourtAvailabilityAsync(court.Id, new DateOnly(2026, 10, 10)));
        Assert.Equal(ErrorCodes.CourtNotFound, ex.Code);
    }

    // 9. maintenance Court is not bookable (generates 0 available slots)
    [Fact]
    public async Task Maintenance_Court_Generates_No_Available_Slots()
    {
        var (db, time, service) = CreateTestHarness(new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.FromHours(7)));
        var (_, center, court) = SeedBaseEntities(db);

        court.Status = CourtStatus.Maintenance;
        db.OperatingHours.Add(new OperatingHour
        {
            SportCenterId = center.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            OpenTime = new TimeOnly(17, 0),
            CloseTime = new TimeOnly(22, 0),
            IsClosed = false
        });
        await db.SaveChangesAsync();

        var result = await service.GetCourtAvailabilityAsync(court.Id, new DateOnly(2026, 10, 10));

        Assert.False(result.IsClosed);
        Assert.Empty(result.Slots);
    }

    // 10. inactive Center is not customer-bookable
    [Fact]
    public async Task Inactive_Center_Throws_NotFoundException()
    {
        var (db, time, service) = CreateTestHarness(new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.FromHours(7)));
        var (_, center, court) = SeedBaseEntities(db);

        center.Status = SportCenterStatus.Inactive;
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetCourtAvailabilityAsync(court.Id, new DateOnly(2026, 10, 10)));
        Assert.Equal(ErrorCodes.CourtNotFound, ex.Code);
    }

    // 11, 12, 13. CourtBlock overlap tests
    [Fact]
    public async Task CourtBlock_Exact_Partial_And_NonOverlapping_Handled_Correctly()
    {
        var (db, time, service) = CreateTestHarness(new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.FromHours(7)));
        var (_, center, court) = SeedBaseEntities(db);

        db.OperatingHours.Add(new OperatingHour
        {
            SportCenterId = center.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            OpenTime = new TimeOnly(17, 0),
            CloseTime = new TimeOnly(21, 0), // 17-18, 18-19, 19-20, 20-21
            IsClosed = false
        });

        var offset = TimeSpan.FromHours(7);
        // Block 1: Exact match on 17-18
        db.CourtBlocks.Add(new CourtBlock
        {
            CourtId = court.Id,
            StartAt = new DateTimeOffset(2026, 10, 10, 17, 0, 0, offset),
            EndAt = new DateTimeOffset(2026, 10, 10, 18, 0, 0, offset),
            Reason = "Exact block"
        });

        // Block 2: Partial overlap across 18:30-19:30 (overlaps 18-19 and 19-20)
        db.CourtBlocks.Add(new CourtBlock
        {
            CourtId = court.Id,
            StartAt = new DateTimeOffset(2026, 10, 10, 18, 30, 0, offset),
            EndAt = new DateTimeOffset(2026, 10, 10, 19, 30, 0, offset),
            Reason = "Partial block"
        });

        // Block 3: Non-overlapping block (22:00-23:00)
        db.CourtBlocks.Add(new CourtBlock
        {
            CourtId = court.Id,
            StartAt = new DateTimeOffset(2026, 10, 10, 22, 0, 0, offset),
            EndAt = new DateTimeOffset(2026, 10, 10, 23, 0, 0, offset),
            Reason = "After hours block"
        });

        await db.SaveChangesAsync();

        var result = await service.GetCourtAvailabilityAsync(court.Id, new DateOnly(2026, 10, 10));

        Assert.Equal(4, result.Slots.Count);
        Assert.Equal(AvailabilitySlotStatus.Blocked, result.Slots[0].Status); // 17-18
        Assert.Equal(AvailabilitySlotStatus.Blocked, result.Slots[1].Status); // 18-19
        Assert.Equal(AvailabilitySlotStatus.Blocked, result.Slots[2].Status); // 19-20
        Assert.Equal(AvailabilitySlotStatus.Available, result.Slots[3].Status); // 20-21
    }

    // 14, 15, 16, 17, 18. BookingSlots status tests
    [Fact]
    public async Task BookingSlots_Statuses_Determined_Accurately()
    {
        // 05:00 UTC = 12:00 local in UTC+7 (well before 17:00 start)
        var currentUtc = new DateTimeOffset(2026, 10, 10, 5, 0, 0, TimeSpan.Zero);
        var (db, time, service) = CreateTestHarness(currentUtc);
        var (_, center, court) = SeedBaseEntities(db);

        var otherCourt = new Court
        {
            Id = Guid.NewGuid(),
            SportCenterId = center.Id,
            Code = "B1",
            Name = "Sân B1",
            BasePricePerHour = 100000m,
            Status = CourtStatus.Active
        };
        db.Courts.Add(otherCourt);

        db.OperatingHours.Add(new OperatingHour
        {
            SportCenterId = center.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            OpenTime = new TimeOnly(17, 0),
            CloseTime = new TimeOnly(22, 0), // 17-18, 18-19, 19-20, 20-21, 21-22
            IsClosed = false
        });

        var offset = TimeSpan.FromHours(7);

        // Slot 17-18: Reserved + IsOccupying = true => Booked
        db.BookingSlots.Add(new BookingSlot
        {
            CourtId = court.Id,
            StartAt = new DateTimeOffset(2026, 10, 10, 17, 0, 0, offset),
            EndAt = new DateTimeOffset(2026, 10, 10, 18, 0, 0, offset),
            ReservationState = ReservationState.Reserved,
            IsOccupying = true
        });

        // Slot 18-19: Valid Held (HoldExpiresAt in future) => Held
        db.BookingSlots.Add(new BookingSlot
        {
            CourtId = court.Id,
            StartAt = new DateTimeOffset(2026, 10, 10, 18, 0, 0, offset),
            EndAt = new DateTimeOffset(2026, 10, 10, 19, 0, 0, offset),
            ReservationState = ReservationState.Held,
            IsOccupying = true,
            HoldExpiresAt = currentUtc.AddMinutes(10)
        });

        // Slot 19-20: Released => Available
        db.BookingSlots.Add(new BookingSlot
        {
            CourtId = court.Id,
            StartAt = new DateTimeOffset(2026, 10, 10, 19, 0, 0, offset),
            EndAt = new DateTimeOffset(2026, 10, 10, 20, 0, 0, offset),
            ReservationState = ReservationState.Released,
            IsOccupying = false
        });

        // Slot 20-21: Expired Held (HoldExpiresAt <= currentUtc) => Available
        db.BookingSlots.Add(new BookingSlot
        {
            CourtId = court.Id,
            StartAt = new DateTimeOffset(2026, 10, 10, 20, 0, 0, offset),
            EndAt = new DateTimeOffset(2026, 10, 10, 21, 0, 0, offset),
            ReservationState = ReservationState.Held,
            IsOccupying = true,
            HoldExpiresAt = currentUtc.AddMinutes(-5) // expired 5 mins ago
        });

        // Slot 21-22 on OTHER court does not affect this court
        db.BookingSlots.Add(new BookingSlot
        {
            CourtId = otherCourt.Id,
            StartAt = new DateTimeOffset(2026, 10, 10, 21, 0, 0, offset),
            EndAt = new DateTimeOffset(2026, 10, 10, 22, 0, 0, offset),
            ReservationState = ReservationState.Reserved,
            IsOccupying = true
        });

        await db.SaveChangesAsync();

        var result = await service.GetCourtAvailabilityAsync(court.Id, new DateOnly(2026, 10, 10));

        Assert.Equal(5, result.Slots.Count);
        Assert.Equal(AvailabilitySlotStatus.Booked, result.Slots[0].Status); // 17-18
        Assert.Equal(AvailabilitySlotStatus.Held, result.Slots[1].Status);   // 18-19
        Assert.Equal(AvailabilitySlotStatus.Available, result.Slots[2].Status); // 19-20
        Assert.Equal(AvailabilitySlotStatus.Available, result.Slots[3].Status); // 20-21 (expired hold)
        Assert.Equal(AvailabilitySlotStatus.Available, result.Slots[4].Status); // 21-22 (other court's booking)
    }

    // 19, 20, 21, 22. Pricing rules resolution
    [Fact]
    public async Task PricingRules_Match_Fallbacks_And_Filters_Correctly()
    {
        var (db, time, service) = CreateTestHarness(new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.FromHours(7)));
        var (_, center, court) = SeedBaseEntities(db);

        db.OperatingHours.Add(new OperatingHour
        {
            SportCenterId = center.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            OpenTime = new TimeOnly(17, 0),
            CloseTime = new TimeOnly(20, 0), // 17-18, 18-19, 19-20
            IsClosed = false
        });

        // Rule 1: Saturday 17:00-18:00 active rule = 150,000 VND
        db.PriceRules.Add(new PriceRule
        {
            CourtId = court.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(18, 0),
            PricePerHour = 150000m,
            IsActive = true
        });

        // Rule 2: Inactive rule for 18:00-19:00 = 180,000 VND -> ignored
        db.PriceRules.Add(new PriceRule
        {
            CourtId = court.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(19, 0),
            PricePerHour = 180000m,
            IsActive = false
        });

        // Rule 3: Sunday rule for 19:00-20:00 -> ignored on Saturday
        db.PriceRules.Add(new PriceRule
        {
            CourtId = court.Id,
            DayOfWeek = CourtGoDayOfWeek.Sunday,
            StartTime = new TimeOnly(19, 0),
            EndTime = new TimeOnly(20, 0),
            PricePerHour = 200000m,
            IsActive = true
        });

        // Rule 4: Expired effective date rule
        db.PriceRules.Add(new PriceRule
        {
            CourtId = court.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(19, 0),
            PricePerHour = 220000m,
            EffectiveTo = new DateOnly(2026, 10, 1),
            IsActive = true
        });

        await db.SaveChangesAsync();

        var result = await service.GetCourtAvailabilityAsync(court.Id, new DateOnly(2026, 10, 10));

        Assert.Equal(3, result.Slots.Count);
        Assert.Equal(150000m, result.Slots[0].Price); // Matched Rule 1
        Assert.Equal(100000m, result.Slots[1].Price); // Fallback to BasePricePerHour (Rule 2 inactive, Rule 4 expired)
        Assert.Equal(100000m, result.Slots[2].Price); // Fallback to BasePricePerHour (Rule 3 is Sunday)
    }

    // 23. Overlapping matching price rules trigger ConfigurationException
    [Fact]
    public async Task Overlapping_PriceRules_Throw_ConfigurationException()
    {
        var (db, time, service) = CreateTestHarness(new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.FromHours(7)));
        var (_, center, court) = SeedBaseEntities(db);

        db.OperatingHours.Add(new OperatingHour
        {
            SportCenterId = center.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            OpenTime = new TimeOnly(17, 0),
            CloseTime = new TimeOnly(18, 0),
            IsClosed = false
        });

        // Two conflicting active rules for the exact same slot
        db.PriceRules.Add(new PriceRule
        {
            CourtId = court.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(18, 0),
            PricePerHour = 150000m,
            IsActive = true
        });

        db.PriceRules.Add(new PriceRule
        {
            CourtId = court.Id,
            DayOfWeek = CourtGoDayOfWeek.Saturday,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(18, 0),
            PricePerHour = 160000m,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConfigurationException>(() =>
            service.GetCourtAvailabilityAsync(court.Id, new DateOnly(2026, 10, 10)));
        Assert.Equal(ErrorCodes.ConfigurationError, ex.Code);
    }

    // 24. Today filtering uses SportCenter timezone
    [Fact]
    public async Task Today_Filtering_Respects_SportCenter_TimeZone()
    {
        // 17:30 UTC on 2026-10-10 is 00:30 on 2026-10-11 in UTC+7 (Ho Chi Minh)
        var utcNow = new DateTimeOffset(2026, 10, 10, 17, 30, 0, TimeSpan.Zero);
        var (db, time, service) = CreateTestHarness(utcNow);
        var (_, center, court) = SeedBaseEntities(db, timeZoneId: "Asia/Ho_Chi_Minh");

        // Requesting 2026-10-10 should fail because in Ho Chi Minh it's already 2026-10-11!
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetCourtAvailabilityAsync(court.Id, new DateOnly(2026, 10, 10)));
        Assert.Equal(ErrorCodes.InvalidAvailabilityDate, ex.Code);
    }

    // 25. Tomorrow morning slots remain visible even when current local time is evening today
    [Fact]
    public async Task Tomorrow_Morning_Slots_Remain_Visible_When_Evening_Today()
    {
        // Local time: 2026-10-10 at 21:00
        var localNow = new DateTimeOffset(2026, 10, 10, 21, 0, 0, TimeSpan.FromHours(7));
        var (db, time, service) = CreateTestHarness(localNow.ToUniversalTime());
        var (_, center, court) = SeedBaseEntities(db);

        db.OperatingHours.Add(new OperatingHour
        {
            SportCenterId = center.Id,
            DayOfWeek = CourtGoDayOfWeek.Sunday,
            OpenTime = new TimeOnly(6, 0),
            CloseTime = new TimeOnly(9, 0), // 06-07, 07-08, 08-09
            IsClosed = false
        });
        await db.SaveChangesAsync();

        var tomorrow = new DateOnly(2026, 10, 11);
        var result = await service.GetCourtAvailabilityAsync(court.Id, tomorrow);

        Assert.Equal(3, result.Slots.Count);
        Assert.Equal(new TimeOnly(6, 0), TimeOnly.FromTimeSpan(result.Slots[0].StartAt.TimeOfDay));
    }
}
