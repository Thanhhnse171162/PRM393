using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Operations;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Domain.Rules;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace CourtGo.Infrastructure.Services;

public class BookingHoldService : IBookingHoldService, IWalkInBookingService
{
    private readonly CourtGoDbContext _db;
    private readonly IExpiredBookingHoldService _expiredHoldService;
    private readonly IBookingCodeGenerator _bookingCodeGenerator;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BookingHoldService> _logger;

    public BookingHoldService(
        CourtGoDbContext db,
        IExpiredBookingHoldService expiredHoldService,
        IBookingCodeGenerator bookingCodeGenerator,
        TimeProvider timeProvider,
        ILogger<BookingHoldService> logger)
    {
        _db = db;
        _expiredHoldService = expiredHoldService;
        _bookingCodeGenerator = bookingCodeGenerator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Task<BookingHoldResponse> HoldAsync(
        Guid customerUserId,
        BookingHoldRequest request,
        CancellationToken cancellationToken = default)
        => CreateBookingAsync(customerUserId, request, null, cancellationToken);

    public async Task<StaffBookingDetail> CreateWalkInAsync(Guid staffId, WalkInBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || request.CustomerName.Length > 150 ||
            string.IsNullOrWhiteSpace(request.CustomerPhone) || request.CustomerPhone.Length is < 8 or > 20 ||
            request.CustomerPhone.Any(c => !char.IsAsciiDigit(c) && c != '+'))
            throw new ValidationException("Valid customer name and phone are required.");
        if (request.PaymentMethod is not ("Cash" or "BankTransfer"))
            throw new ValidationException("Walk-in payment must be Cash or BankTransfer.");
        var result = await CreateBookingAsync(staffId, new(request.CourtId, request.SlotStartAts), request, cancellationToken);
        return await new StaffOperationsService(_db, _timeProvider).GetBookingAsync(staffId, result.BookingId, cancellationToken);
    }

    private async Task<BookingHoldResponse> CreateBookingAsync(Guid customerUserId, BookingHoldRequest request,
        WalkInBookingRequest? walkIn, CancellationToken cancellationToken)
    {
        // 1. Validate input DTO
        if (request is null || request.CourtId == Guid.Empty || request.SlotStartAts is null || request.SlotStartAts.Count == 0)
        {
            throw new ValidationException("SlotStartAts must not be empty.", ErrorCodes.InvalidSlotSelection);
        }

        // Duplicate check
        if (request.SlotStartAts.Distinct().Count() != request.SlotStartAts.Count)
        {
            throw new ValidationException("Duplicate slot start times are not allowed.", ErrorCodes.InvalidSlotSelection);
        }

        // Sort ascending
        var orderedStarts = request.SlotStartAts.OrderBy(s => s).ToList();

        // Check hour alignment
        if (orderedStarts.Any(s => s.Minute != 0 || s.Second != 0 || s.Millisecond != 0))
        {
            throw new ValidationException("Slots must start at the beginning of an hour (minute 0).", ErrorCodes.InvalidSlotSelection);
        }

        // Check consecutive
        if (!SlotRules.AreConsecutive(orderedStarts))
        {
            throw new ValidationException("Selected slots must be consecutive 1-hour slots.", ErrorCodes.InvalidSlotSelection);
        }

        // 2. Validate Customer User
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == customerUserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException($"User with ID '{customerUserId}' was not found.", ErrorCodes.NotFound);
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("Account is inactive.", ErrorCodes.AccountInactive);
        }

        if (walkIn is null && user.Role != UserRole.Customer)
        {
            throw new ForbiddenException("Only customers can hold bookings.", ErrorCodes.Forbidden);
        }

        // Cleanup uses booking locks; finish it before acquiring the court lock.
        await _expiredHoldService.ReleaseExpiredHoldsAsync(cancellationToken);
        await using var lease = await CourtMutationLease.AcquireAsync(_db, request.CourtId, cancellationToken);
        if (walkIn is not null)
        {
            var center = await StaffAccess.GetCenterAsync(_db, customerUserId, cancellationToken);
            if (!await _db.Courts.AnyAsync(c => c.Id == request.CourtId && c.SportCenterId == center, cancellationToken))
                throw new ForbiddenException("Court belongs to another center.", ErrorCodes.StaffCenterAccessDenied);
        }
            // 3. Load Court, Center, Sport
            var court = await _db.Courts
            .Include(c => c.SportCenter)
            .Include(c => c.Sport)
            .FirstOrDefaultAsync(c => c.Id == request.CourtId, cancellationToken);

        if (court is null)
        {
            throw new NotFoundException($"Court with ID '{request.CourtId}' was not found.", ErrorCodes.CourtNotFound);
        }

        if (court.SportCenter is null || court.SportCenter.Status != SportCenterStatus.Active)
        {
            throw new ConflictException("Sport center is inactive or not found.", ErrorCodes.CourtNotBookable);
        }

        if (court.Sport is null || !court.Sport.IsActive)
        {
            throw new ConflictException("Sport is inactive or not found.", ErrorCodes.CourtNotBookable);
        }

        if (court.Status != CourtStatus.Active)
        {
            throw new ConflictException("Court is not bookable.", ErrorCodes.CourtNotBookable);
        }

        // 4. Timezone and Local Date Validation
        var tz = TimeZoneHelper.ResolveTimeZone(court.SportCenter.TimeZoneId);
        var currentUtc = _timeProvider.GetUtcNow();
        var currentCenterTime = TimeZoneInfo.ConvertTime(currentUtc, tz);
        var currentCenterDate = DateOnly.FromDateTime(currentCenterTime.DateTime);

        var localStarts = orderedStarts.Select(s => TimeZoneInfo.ConvertTime(s, tz)).ToList();
        var localDates = localStarts.Select(s => DateOnly.FromDateTime(s.DateTime)).Distinct().ToList();

        if (localDates.Count != 1)
        {
            throw new ValidationException("All selected slots must be on the same calendar day in the sport center's time zone.", ErrorCodes.InvalidSlotSelection);
        }

        var bookingDate = localDates[0];

        if (bookingDate < currentCenterDate)
        {
            throw new ValidationException("Requested date cannot be in the past.", ErrorCodes.InvalidSlotSelection);
        }

        // 6. Load System Settings
        var settings = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.Id == 1, cancellationToken);

        if (settings is null)
        {
            _logger.LogError("System settings row (Id = 1) is missing from database.");
            throw new ConfigurationException("System settings row (Id = 1) is missing.", ErrorCodes.SystemSettingsNotFound);
        }

        // 7. Check Past Slot & Minimum Lead Time
        if (bookingDate == currentCenterDate)
        {
            var minAllowedStart = currentCenterTime.AddMinutes(walkIn is null ? settings.MinBookingLeadMinutes : 0);
            foreach (var localStart in localStarts)
            {
                if (localStart < minAllowedStart)
                {
                    throw new ConflictException("Selected slot is in the past or does not meet the minimum booking lead time.", ErrorCodes.BookingSlotConflict);
                }
            }
        }

        // 8. Operating Hours & Operating Hour Exceptions
        var exception = await _db.OperatingHourExceptions
            .FirstOrDefaultAsync(e => e.SportCenterId == court.SportCenterId && e.Date == bookingDate, cancellationToken);

        bool isClosed;
        TimeOnly? openTime = null;
        TimeOnly? closeTime = null;

        if (exception is not null)
        {
            if (exception.IsClosed)
            {
                isClosed = true;
            }
            else
            {
                isClosed = false;
                openTime = exception.OpenTime;
                closeTime = exception.CloseTime;
            }
        }
        else
        {
            var courtGoDay = bookingDate.DayOfWeek.ToCourtGoDayOfWeek();
            var regularHours = await _db.OperatingHours
                .FirstOrDefaultAsync(o => o.SportCenterId == court.SportCenterId && o.DayOfWeek == courtGoDay, cancellationToken);

            if (regularHours is null || regularHours.IsClosed)
            {
                isClosed = true;
            }
            else
            {
                isClosed = false;
                openTime = regularHours.OpenTime;
                closeTime = regularHours.CloseTime;
            }
        }

        if (isClosed || openTime is null || closeTime is null || openTime >= closeTime)
        {
            throw new ConflictException("Sport center is closed on the selected date.", ErrorCodes.CourtNotBookable);
        }

        foreach (var localStart in localStarts)
        {
            var slotLocalStart = TimeOnly.FromDateTime(localStart.DateTime);
            var slotLocalEnd = slotLocalStart.AddHours(1);

            if (slotLocalStart < openTime.Value || slotLocalEnd > closeTime.Value)
            {
                throw new ValidationException("Selected slots are outside sport center operating hours.", ErrorCodes.InvalidSlotSelection);
            }
        }

        // 9. Recheck Court Blocks
        var windowStart = orderedStarts.First();
        var windowEnd = orderedStarts.Last().AddHours(1);

        var overlappingBlocks = await _db.CourtBlocks
            .Where(b => b.CourtId == court.Id && b.StartAt < windowEnd && b.EndAt > windowStart)
            .ToListAsync(cancellationToken);

        foreach (var start in orderedStarts)
        {
            var end = start.AddHours(1);
            if (overlappingBlocks.Any(b => b.StartAt < end && b.EndAt > start))
            {
                throw new ConflictException("Khung giờ đã bị khóa.", ErrorCodes.BookingSlotConflict);
            }
        }

        // 10. Recheck Occupying Booking Slots
        var occupyingSlots = await _db.BookingSlots
            .Where(bs => bs.CourtId == court.Id && bs.StartAt < windowEnd && bs.EndAt > windowStart && bs.IsOccupying)
            .ToListAsync(cancellationToken);

        foreach (var start in orderedStarts)
        {
            var end = start.AddHours(1);
            var conflict = occupyingSlots.FirstOrDefault(bs => bs.StartAt < end && bs.EndAt > start);
            if (conflict is not null)
            {
                if (conflict.ReservationState == ReservationState.Held)
                {
                    if (conflict.HoldExpiresAt.HasValue && conflict.HoldExpiresAt.Value > currentUtc)
                    {
                        throw new ConflictException("Khung giờ vừa được người khác chọn.", ErrorCodes.BookingSlotConflict);
                    }
                }
                else if (conflict.ReservationState == ReservationState.Reserved)
                {
                    throw new ConflictException("Khung giờ đã được đặt.", ErrorCodes.BookingSlotConflict);
                }
            }
        }

        // 11. Recalculate Prices
        var dayOfWeekCourtGo = bookingDate.DayOfWeek.ToCourtGoDayOfWeek();
        var priceRules = await _db.PriceRules
            .Where(pr => pr.CourtId == court.Id && pr.IsActive && pr.DayOfWeek == dayOfWeekCourtGo)
            .Where(pr => pr.EffectiveFrom == null || bookingDate >= pr.EffectiveFrom)
            .Where(pr => pr.EffectiveTo == null || bookingDate <= pr.EffectiveTo)
            .ToListAsync(cancellationToken);

        var slotCalculations = new List<(DateTimeOffset StartAt, DateTimeOffset EndAt, decimal UnitPrice, Guid? PriceRuleId)>();

        for (int i = 0; i < orderedStarts.Count; i++)
        {
            var startAt = orderedStarts[i];
            var endAt = startAt.AddHours(1);
            var localStart = localStarts[i];
            var slotLocalStart = TimeOnly.FromDateTime(localStart.DateTime);
            var slotLocalEnd = slotLocalStart.AddHours(1);

            var matchingRules = priceRules
                .Where(r => r.StartTime <= slotLocalStart && r.EndTime >= slotLocalEnd)
                .ToList();

            if (matchingRules.Count > 1)
            {
                _logger.LogError("Multiple active price rules found for Court {CourtId} at slot {StartAt}", court.Id, startAt);
                throw new ConfigurationException($"Multiple active price rules match court '{court.Id}' at slot '{startAt}'.", ErrorCodes.PricingConfigurationError);
            }

            decimal unitPrice = matchingRules.Count == 1 ? matchingRules[0].PricePerHour : court.BasePricePerHour;
            Guid? priceRuleId = matchingRules.Count == 1 ? matchingRules[0].Id : null;

            slotCalculations.Add((startAt, endAt, unitPrice, priceRuleId));
        }

        decimal totalAmount = slotCalculations.Sum(s => s.UnitPrice);
        decimal depositPercent = walkIn is null ? settings.DefaultDepositPercent : 0;
        decimal depositAmount = PaymentRules.CalculateDeposit(totalAmount, depositPercent / 100m);
        decimal remainingAmount = PaymentRules.CalculateRemaining(totalAmount, depositAmount);

        // 12. Active Cancellation Policy
        var activePolicy = await _db.CancellationPolicies
            .Where(p => p.IsActive && p.EffectiveFrom <= currentUtc && (p.EffectiveTo == null || p.EffectiveTo >= currentUtc))
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(cancellationToken);

        // 13. Create Entities
        var holdExpiresAt = currentUtc.AddMinutes(settings.HoldDurationMinutes);
        var bookingStartAt = slotCalculations.First().StartAt;
        var bookingEndAt = slotCalculations.Last().EndAt;
        var durationMinutes = (int)(bookingEndAt - bookingStartAt).TotalMinutes;
        var bookingCode = _bookingCodeGenerator.GenerateBookingCode(bookingDate);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingCode = bookingCode,
            CustomerUserId = walkIn is null ? user.Id : null,
            CustomerNameSnapshot = walkIn?.CustomerName.Trim() ?? user.FullName,
            CustomerPhoneSnapshot = walkIn?.CustomerPhone.Trim() ?? user.PhoneNumber,
            CustomerEmailSnapshot = walkIn is null ? user.Email : null,
            CourtId = court.Id,
            CourtNameSnapshot = court.Name,
            CenterNameSnapshot = court.SportCenter.Name,
            SportNameSnapshot = court.Sport.Name,
            Source = walkIn is null ? BookingSource.Online : BookingSource.WalkIn,
            StartAt = bookingStartAt,
            EndAt = bookingEndAt,
            DurationMinutes = durationMinutes,
            TotalAmount = totalAmount,
            DepositPercentSnapshot = depositPercent,
            DepositAmount = depositAmount,
            BookingStatus = walkIn is null ? BookingStatus.PendingPayment : BookingStatus.Confirmed,
            PaymentStatus = walkIn is null ? BookingPaymentStatus.Unpaid : BookingPaymentStatus.FullyPaid,
            HoldExpiresAt = walkIn is null ? holdExpiresAt : null,
            QrToken = walkIn is null ? null : new QrTokenGenerator().GenerateToken(),
            CancellationPolicyId = activePolicy?.Id,
            CreatedByUserId = user.Id,
            CreatedAt = currentUtc
        };

        foreach (var sc in slotCalculations)
        {
            var slot = new BookingSlot
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                CourtId = court.Id,
                StartAt = sc.StartAt,
                EndAt = sc.EndAt,
                UnitPrice = sc.UnitPrice,
                PriceRuleId = sc.PriceRuleId,
                ReservationState = walkIn is null ? ReservationState.Held : ReservationState.Reserved,
                IsOccupying = true,
                HoldExpiresAt = walkIn is null ? holdExpiresAt : null,
                CreatedAt = currentUtc
            };
            booking.Slots.Add(slot);
        }

        var history = new BookingStatusHistory
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            FromStatus = null,
            ToStatus = booking.BookingStatus,
            ChangedByUserId = user.Id,
            Reason = walkIn is null ? "Customer created booking hold" : "Staff created fully paid walk-in",
            CreatedAt = currentUtc
        };
        booking.StatusHistories.Add(history);
        if (walkIn is not null && totalAmount > 0)
            booking.Payments.Add(new Payment
            {
                BookingId = booking.Id, PaymentKind = PaymentKind.Remaining,
                PaymentMethod = Enum.Parse<PaymentMethod>(walkIn.PaymentMethod),
                Amount = totalAmount, TransactionStatus = PaymentTransactionStatus.Succeeded,
                ConfirmedByUserId = user.Id, PaidAt = currentUtc, CreatedAt = currentUtc
            });

        // 14. Atomic Persistence & Transaction
        IDbContextTransaction? tx = null;
        if (_db.Database.IsRelational() && _db.Database.CurrentTransaction is null)
        {
            tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync(cancellationToken);

            if (tx is not null)
            {
                await tx.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateException ex)
        {
            if (tx is not null)
            {
                await tx.RollbackAsync(cancellationToken);
            }

            if (IsUniqueSlotConflict(ex))
            {
                _logger.LogWarning(ex, "Race condition detected: unique index conflict on Court {CourtId}", court.Id);
                throw new ConflictException("Khung giờ vừa được người khác chọn.", ErrorCodes.BookingSlotConflict);
            }

            throw;
        }
        finally
        {
            if (tx is not null)
            {
                await tx.DisposeAsync();
            }
        }

        await lease.CommitAsync(cancellationToken);
        return new BookingHoldResponse(
            booking.Id,
            booking.BookingCode,
            booking.BookingStatus.ToString(),
            booking.PaymentStatus.ToString(),
            new BookingHoldCourtDto(court.Id, court.Code, court.Name),
            new BookingHoldCenterDto(court.SportCenter.Id, court.SportCenter.Name),
            new BookingHoldSportDto(court.Sport.Id, court.Sport.Name),
            booking.StartAt,
            booking.EndAt,
            booking.DurationMinutes,
            booking.Slots.Select(s => new BookingHoldSlotDto(s.StartAt, s.EndAt, s.UnitPrice)).ToList(),
            booking.TotalAmount,
            booking.DepositPercentSnapshot,
            booking.DepositAmount,
            remainingAmount,
            holdExpiresAt
        );
    }

    private static bool IsUniqueSlotConflict(DbUpdateException ex)
    {
        if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            // 2601: Cannot insert duplicate key row in object with unique index
            // 2627: Violation of PRIMARY KEY / UNIQUE KEY constraint
            if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
            {
                return true;
            }
        }

        var message = ex.ToString();
        return message.Contains("UX_BookingSlots_ActiveCourtStart", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UQ_Bookings_BookingCode", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
    }
}
