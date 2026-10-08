using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Domain.Rules;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class StaffBookingService(CourtGoDbContext dbContext, BookingCommandExecutor commands, TimeProvider timeProvider)
    : IStaffBookingService
{
    private readonly CourtGoDbContext _dbContext = dbContext;
    private readonly BookingCommandExecutor _commands = commands;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<StaffQrVerificationResponse> VerifyQrAsync(
        Guid staffUserId, StaffQrVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var centerId = await GetStaffCenterAsync(staffUserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.QrToken) || request.QrToken.Length > 150)
            throw new NotFoundException("QR is invalid.", ErrorCodes.QrInvalid);
        var booking = await _dbContext.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(b => b.QrToken == request.QrToken, cancellationToken);
        // SQL Server's default collation can compare tokens case-insensitively.
        if (booking is null || !string.Equals(booking.QrToken, request.QrToken, StringComparison.Ordinal))
            throw new NotFoundException("QR is invalid.", ErrorCodes.QrInvalid);
        await ValidateCenterAsync(booking, centerId, cancellationToken);
        ValidateCheckInState(booking);
        var paid = await GetPaidAmountAsync(booking.Id, cancellationToken);
        var remaining = PaymentRules.CalculateRemaining(booking.TotalAmount, paid);
        return new(booking.Id, booking.BookingCode,
            new(booking.CustomerNameSnapshot, booking.CustomerPhoneSnapshot),
            new(booking.CenterNameSnapshot), new(booking.CourtId, booking.CourtNameSnapshot),
            new(booking.SportNameSnapshot), booking.StartAt, booking.EndAt, booking.DurationMinutes,
            booking.BookingStatus.ToString(), booking.PaymentStatus.ToString(),
            new(booking.TotalAmount, booking.DepositAmount, paid, remaining),
            remaining > 0, booking.BookingStatus == BookingStatus.CheckedIn);
    }

    public Task<CollectRemainingPaymentResponse> CollectRemainingPaymentAsync(
        Guid staffUserId, Guid bookingId, CollectRemainingPaymentRequest request, CancellationToken cancellationToken = default)
        => _commands.ExecuteAsync(bookingId, async (booking, ct) =>
        {
            await ValidateCenterAsync(booking, await GetStaffCenterAsync(staffUserId, ct), ct);
            if (booking.BookingStatus is not (BookingStatus.Confirmed or BookingStatus.CheckedIn or BookingStatus.InProgress))
                throw new ConflictException("Booking cannot receive remaining payment.", ErrorCodes.BookingNotCheckInEligible);
            var method = ParseManualPaymentMethod(request.PaymentMethod);
            var paid = await GetPaidAmountAsync(booking.Id, ct);
            var remaining = PaymentRules.CalculateRemaining(booking.TotalAmount, paid);
            if (remaining == 0)
                throw new ConflictException("Booking is already fully paid.", ErrorCodes.PaymentAlreadyFullyPaid);
            var now = _timeProvider.GetUtcNow();
            var payment = new Payment
            {
                BookingId = booking.Id, PaymentKind = PaymentKind.Remaining, PaymentMethod = method,
                Amount = remaining, TransactionStatus = PaymentTransactionStatus.Succeeded,
                PaidByUserId = booking.CustomerUserId, ConfirmedByUserId = staffUserId,
                PaidAt = now, CreatedAt = now, ProviderTransactionId = null
            };
            _dbContext.Payments.Add(payment);
            // Persist inside the same transaction before recalculating the authoritative aggregate.
            await _dbContext.SaveChangesAsync(ct);
            paid = await GetPaidAmountAsync(booking.Id, ct);
            remaining = PaymentRules.CalculateRemaining(booking.TotalAmount, paid);
            if (remaining == 0)
                booking.PaymentStatus = BookingPaymentStatus.FullyPaid;
            return new CollectRemainingPaymentResponse(booking.Id, payment.Id, method.ToString(), payment.Amount,
                booking.PaymentStatus.ToString(), paid, remaining);
        }, cancellationToken);

    public Task<CheckInResponse> CheckInAsync(
        Guid staffUserId, Guid bookingId, CreateCheckInRequest request, CancellationToken cancellationToken = default)
        => _commands.ExecuteAsync(bookingId, async (booking, ct) =>
        {
            await ValidateCenterAsync(booking, await GetStaffCenterAsync(staffUserId, ct), ct);
            if (string.IsNullOrWhiteSpace(request.QrToken) ||
                !string.Equals(request.QrToken, booking.QrToken, StringComparison.Ordinal))
                throw new ValidationException("QR is invalid.", ErrorCodes.QrInvalid);
            ValidateCheckInState(booking);
            if (booking.BookingStatus == BookingStatus.CheckedIn)
            {
                var existing = await _dbContext.CheckIns.AsNoTracking().SingleOrDefaultAsync(c => c.BookingId == booking.Id, ct);
                if (existing is null)
                    throw new ConflictException("Booking has already been checked in.", ErrorCodes.CheckInAlreadyCompleted);
                return ToResponse(booking, existing);
            }

            var paid = await GetPaidAmountAsync(booking.Id, ct);
            var remaining = PaymentRules.CalculateRemaining(booking.TotalAmount, paid);
            var overridePayment = false;
            if (remaining > 0)
            {
                var setting = await _dbContext.SystemSettings.AsNoTracking().SingleOrDefaultAsync(s => s.Id == 1, ct)
                    ?? throw new ConfigurationException("System settings row is missing.", ErrorCodes.SystemSettingsNotFound);
                if (!setting.AllowOutstandingCheckIn || !request.AllowOutstandingPayment ||
                    string.IsNullOrWhiteSpace(request.OverrideReason))
                    throw new ConflictException("Collect the remaining payment or provide an authorized explicit override.",
                        ErrorCodes.OutstandingPaymentRequired);
                if (request.OverrideReason.Length > 500)
                    throw new ValidationException("overrideReason must not exceed 500 characters.");
                overridePayment = true;
            }
            var now = _timeProvider.GetUtcNow();
            var checkIn = new CheckIn
            {
                BookingId = booking.Id, StaffUserId = staffUserId, Method = CheckInMethod.QR, CheckedInAt = now,
                OutstandingPaymentOverride = overridePayment,
                OverrideReason = overridePayment ? request.OverrideReason!.Trim() : null
            };
            _dbContext.CheckIns.Add(checkIn);
            booking.BookingStatus = BookingStatus.CheckedIn;
            if (remaining == 0)
                booking.PaymentStatus = BookingPaymentStatus.FullyPaid;
            _dbContext.BookingStatusHistories.Add(new BookingStatusHistory
            {
                BookingId = booking.Id, FromStatus = BookingStatus.Confirmed, ToStatus = BookingStatus.CheckedIn,
                ChangedByUserId = staffUserId, Reason = "Staff QR check-in", CreatedAt = now
            });
            if (booking.CustomerUserId is Guid customerId)
                _dbContext.Notifications.Add(new Notification
                {
                    UserId = customerId, Type = NotificationType.Booking, Title = "Check-in thành công",
                    Message = $"Đã check-in lịch đặt {booking.BookingCode}.",
                    ReferenceType = "Booking", ReferenceId = booking.Id, CreatedAt = now
                });
            return ToResponse(booking, checkIn);
        }, cancellationToken);

    private Task<Guid> GetStaffCenterAsync(Guid staffUserId, CancellationToken cancellationToken)
        => StaffAccess.GetCenterAsync(_dbContext, staffUserId, cancellationToken);

    private async Task ValidateCenterAsync(Booking booking, Guid centerId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Courts.AsNoTracking().AnyAsync(c => c.Id == booking.CourtId && c.SportCenterId == centerId, cancellationToken))
            throw new ForbiddenException("Booking belongs to another center.", ErrorCodes.StaffCenterAccessDenied);
    }

    private Task<decimal> GetPaidAmountAsync(Guid bookingId, CancellationToken cancellationToken)
        => BookingPaymentQueries.GetPaidAmountAsync(_dbContext, bookingId, cancellationToken);

    private static PaymentMethod ParseManualPaymentMethod(string? method) => method?.ToLowerInvariant() switch
    {
        "cash" => PaymentMethod.Cash,
        "banktransfer" => PaymentMethod.BankTransfer,
        _ => throw new ValidationException("Only Cash and BankTransfer are supported.", ErrorCodes.UnsupportedPaymentMethod)
    };

    private static void ValidateCheckInState(Booking booking)
    {
        if (booking.BookingStatus is not (BookingStatus.Confirmed or BookingStatus.CheckedIn))
            throw new ConflictException("Booking is not eligible for check-in.", ErrorCodes.BookingNotCheckInEligible);
    }

    private static CheckInResponse ToResponse(Booking booking, CheckIn checkIn) =>
        new(booking.Id, booking.BookingCode, booking.BookingStatus.ToString(), booking.PaymentStatus.ToString(),
            checkIn.CheckedInAt, checkIn.StaffUserId, checkIn.OutstandingPaymentOverride,
            booking.CourtNameSnapshot, booking.StartAt, booking.EndAt);
}
