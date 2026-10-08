using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Domain.Rules;
using CourtGo.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public sealed class DepositPaymentService(CourtGoDbContext dbContext, BookingCommandExecutor commands,
    IExpiredBookingHoldService expiredHoldService, IPaymentGateway gateway, IQrTokenGenerator qrTokenGenerator,
    TimeProvider timeProvider) : IDepositPaymentService
{
    private readonly CourtGoDbContext _dbContext = dbContext;
    private readonly BookingCommandExecutor _commands = commands;
    private readonly IExpiredBookingHoldService _expiredHoldService = expiredHoldService;
    private readonly IPaymentGateway _gateway = gateway;
    private readonly IQrTokenGenerator _qrTokenGenerator = qrTokenGenerator;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<DepositPaymentResponse> StartDepositAsync(Guid customerUserId, Guid bookingId,
        StartDepositPaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.Bookings.AsNoTracking().AnyAsync(
            b => b.Id == bookingId && b.CustomerUserId == customerUserId, cancellationToken))
            throw BookingNotFound();
        await _expiredHoldService.ReleaseExpiredHoldsAsync(cancellationToken);
        return await WithUniqueConflictHandlingAsync(() => _commands.ExecuteAsync(bookingId, async (booking, ct) =>
        {
            ValidateOwner(booking, customerUserId);
            var attempts = await _dbContext.Payments.AsNoTracking()
                .Where(p => p.BookingId == booking.Id && p.PaymentKind == PaymentKind.Deposit).ToListAsync(ct);
            if (attempts.Any(p => p.TransactionStatus == PaymentTransactionStatus.Succeeded))
                throw new ConflictException("Deposit has already succeeded.", ErrorCodes.PaymentAlreadyCompleted);
            var slots = await _commands.ReloadSlotsAsync(booking.Id, ct);
            ValidateHold(booking, slots, _timeProvider.GetUtcNow(), ErrorCodes.BookingHoldExpired);
            var method = ParseMethod(request.PaymentMethod);
            if (!_gateway.Supports(method))
                throw new ValidationException("The configured gateway does not support this payment method.", ErrorCodes.UnsupportedPaymentMethod);
            if (booking.DepositAmount <= 0 || booking.DepositAmount > booking.TotalAmount)
                throw new ConflictException("Booking has an invalid deposit amount.", ErrorCodes.PaymentAmountMismatch);

            var pending = attempts.Where(p => p.TransactionStatus == PaymentTransactionStatus.Pending).ToList();
            if (pending.Count > 1)
                throw new ConflictException("Multiple pending deposits require reconciliation.", ErrorCodes.PaymentStateConflict);
            if (pending.Count == 1)
            {
                var existing = pending[0];
                if (existing.Amount != booking.DepositAmount)
                    throw new ConflictException("Pending payment amount differs from the booking snapshot.", ErrorCodes.PaymentAmountMismatch);
                var attempt = await _gateway.CreateDepositAsync(
                    new(existing.Id, booking.Id, existing.PaymentMethod, existing.Amount), ct);
                if (!string.Equals(existing.ProviderTransactionId, attempt.ProviderTransactionId, StringComparison.Ordinal))
                    throw new ConflictException("Pending payment reference is inconsistent.", ErrorCodes.PaymentStateConflict);
                return ToStartResponse(booking, existing, attempt);
            }

            var payment = new Payment
            {
                BookingId = booking.Id, PaymentKind = PaymentKind.Deposit, PaymentMethod = method,
                Amount = booking.DepositAmount, TransactionStatus = PaymentTransactionStatus.Pending,
                PaidByUserId = customerUserId, CreatedAt = _timeProvider.GetUtcNow()
            };
            var created = await _gateway.CreateDepositAsync(new(payment.Id, booking.Id, method, payment.Amount), ct);
            payment.ProviderTransactionId = created.ProviderTransactionId;
            _dbContext.Payments.Add(payment);
            return ToStartResponse(booking, payment, created);
        }, cancellationToken));
    }

    public async Task<DepositConfirmationResponse> ApplyVerifiedResultAsync(VerifiedPaymentResult result,
        Guid? customerUserId = null, CancellationToken cancellationToken = default)
    {
        // Only the trusted gateway adapter constructs this value; HTTP controllers never bind it.
        var bookingId = await _dbContext.Payments.AsNoTracking()
            .Where(p => p.Id == result.PaymentId &&
                (customerUserId == null || p.Booking!.CustomerUserId == customerUserId))
            .Select(p => (Guid?)p.BookingId).SingleOrDefaultAsync(cancellationToken)
            ?? throw PaymentNotFound();
        return await WithUniqueConflictHandlingAsync(() => _commands.ExecuteAsync(bookingId, async (booking, ct) =>
        {
            if (customerUserId.HasValue && booking.CustomerUserId != customerUserId)
                throw PaymentNotFound();
            var payment = await _dbContext.Payments.SingleOrDefaultAsync(p => p.Id == result.PaymentId, ct)
                ?? throw PaymentNotFound();
            await _dbContext.Entry(payment).ReloadAsync(ct);
            if (payment.BookingId != booking.Id ||
                payment.PaymentKind is not (PaymentKind.Deposit or PaymentKind.Refund))
                throw new ConflictException("Payment is not this booking's deposit or refund.", ErrorCodes.PaymentStateConflict);
            if (result.Amount != payment.Amount || payment.Amount <= 0)
                throw new ConflictException("Verified amount differs from the payment record.", ErrorCodes.PaymentAmountMismatch);
            if (payment.PaymentKind == PaymentKind.Deposit && payment.Amount != booking.DepositAmount)
                throw new ConflictException("Verified amount differs from the deposit snapshot.", ErrorCodes.PaymentAmountMismatch);
            if (string.IsNullOrWhiteSpace(result.ProviderTransactionId) || result.ProviderTransactionId.Length > 200 ||
                (payment.ProviderTransactionId != null &&
                 !string.Equals(payment.ProviderTransactionId, result.ProviderTransactionId, StringComparison.Ordinal)))
                throw new ValidationException("Provider reference verification failed.", ErrorCodes.PaymentVerificationFailed);
            if (result.TransactionStatus is not (PaymentTransactionStatus.Succeeded or PaymentTransactionStatus.Failed or PaymentTransactionStatus.Cancelled))
                throw new ValidationException("Provider result is invalid.", ErrorCodes.PaymentVerificationFailed);

            if (payment.TransactionStatus != PaymentTransactionStatus.Pending)
            {
                if (payment.TransactionStatus == result.TransactionStatus)
                    return await ToConfirmationResponseAsync(booking, payment, ct);
                throw new ConflictException("Payment has already been finalized.", ErrorCodes.PaymentAlreadyFinalized);
            }

            if (payment.PaymentKind == PaymentKind.Deposit)
            {
                if (result.TransactionStatus == PaymentTransactionStatus.Succeeded)
                {
                    var slots = await _commands.ReloadSlotsAsync(booking.Id, ct);
                    ValidateHold(booking, slots, _timeProvider.GetUtcNow(), ErrorCodes.PaymentAfterHoldExpired);
                    if (await _dbContext.Payments.AsNoTracking().AnyAsync(p => p.BookingId == booking.Id &&
                        p.PaymentKind == PaymentKind.Deposit && p.TransactionStatus == PaymentTransactionStatus.Succeeded, ct))
                        throw new ConflictException("Deposit has already succeeded.", ErrorCodes.PaymentAlreadyCompleted);
                    var now = _timeProvider.GetUtcNow();
                    payment.TransactionStatus = PaymentTransactionStatus.Succeeded;
                    payment.PaidAt = now;
                    booking.BookingStatus = BookingStatus.Confirmed;
                    booking.PaymentStatus = BookingPaymentStatus.DepositPaid;
                    booking.HoldExpiresAt = null;
                    booking.QrToken = _qrTokenGenerator.GenerateToken();
                    foreach (var slot in slots)
                    {
                        slot.ReservationState = ReservationState.Reserved;
                        slot.IsOccupying = true;
                        slot.HoldExpiresAt = null;
                    }
                    _dbContext.BookingStatusHistories.Add(new BookingStatusHistory
                    {
                        BookingId = booking.Id, FromStatus = BookingStatus.PendingPayment, ToStatus = BookingStatus.Confirmed,
                        ChangedByUserId = customerUserId, Reason = "Deposit payment succeeded", CreatedAt = now
                    });
                    if (booking.CustomerUserId is Guid recipient)
                        _dbContext.Notifications.Add(new Notification
                        {
                            UserId = recipient, Type = NotificationType.Booking, Title = "Đặt sân thành công",
                            Message = $"Lịch đặt {booking.BookingCode} đã được xác nhận.",
                            ReferenceType = "Booking", ReferenceId = booking.Id, CreatedAt = now
                        });
                }
                else
                {
                    payment.TransactionStatus = result.TransactionStatus;
                }
            }
            else // PaymentKind.Refund
            {
                if (result.TransactionStatus == PaymentTransactionStatus.Succeeded)
                {
                    var now = _timeProvider.GetUtcNow();
                    payment.TransactionStatus = PaymentTransactionStatus.Succeeded;
                    payment.PaidAt = now;

                    var totalCharges = await _dbContext.Payments.AsNoTracking()
                        .Where(PaymentRules.SuccessfulCharge)
                        .Where(p => p.BookingId == booking.Id)
                        .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

                    var totalRefunds = await _dbContext.Payments.AsNoTracking()
                        .Where(p => p.BookingId == booking.Id && p.PaymentKind == PaymentKind.Refund &&
                               (p.TransactionStatus == PaymentTransactionStatus.Succeeded || p.Id == payment.Id))
                        .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

                    if (totalRefunds >= totalCharges)
                        booking.PaymentStatus = BookingPaymentStatus.Refunded;
                    else
                        booking.PaymentStatus = BookingPaymentStatus.PartiallyRefunded;

                    if (booking.CustomerUserId is Guid recipient)
                    {
                        _dbContext.Notifications.Add(new Notification
                        {
                            UserId = recipient,
                            Type = NotificationType.Payment,
                            Title = "Hoàn tiền thành công",
                            Message = $"Đã hoàn tiền {payment.Amount:N0} đ cho lịch đặt {booking.BookingCode}.",
                            ReferenceType = "Payment",
                            ReferenceId = payment.Id,
                            CreatedAt = now
                        });
                    }
                }
                else
                {
                    payment.TransactionStatus = result.TransactionStatus;
                }
            }
            payment.ProviderTransactionId = result.ProviderTransactionId;
            // All writes and response aggregates are inside BookingCommandExecutor's transaction.
            await _dbContext.SaveChangesAsync(ct);
            return await ToConfirmationResponseAsync(booking, payment, ct);
        }, cancellationToken));
    }

    public async Task<BookingPaymentStatusResponse> GetPaymentStatusAsync(Guid customerUserId, Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.Bookings.AsNoTracking()
            .Where(b => b.Id == bookingId && b.CustomerUserId == customerUserId)
            .Select(b => new
            {
                b.Id, b.BookingCode, b.BookingStatus, b.PaymentStatus, b.TotalAmount, b.DepositAmount,
                PaidAmount = _dbContext.Payments.Where(PaymentRules.SuccessfulCharge)
                    .Where(p => p.BookingId == b.Id).Sum(p => (decimal?)p.Amount) ?? 0m,
                Payments = b.Payments.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id)
                    .Select(p => new PaymentTransactionDto(p.Id, p.PaymentKind.ToString(), p.PaymentMethod.ToString(),
                        p.Amount, p.TransactionStatus.ToString(), p.PaidAt)).ToList()
            }).SingleOrDefaultAsync(cancellationToken) ?? throw BookingNotFound();
        return new(row.Id, row.BookingCode, row.BookingStatus.ToString(), row.PaymentStatus.ToString(),
            row.TotalAmount, row.DepositAmount, row.PaidAmount,
            PaymentRules.CalculateRemaining(row.TotalAmount, row.PaidAmount), row.Payments);
    }

    private async Task<DepositConfirmationResponse> ToConfirmationResponseAsync(Booking booking, Payment payment, CancellationToken ct)
    {
        var paid = await BookingPaymentQueries.GetPaidAmountAsync(_dbContext, booking.Id, ct);
        var depositPaid = await _dbContext.Payments.AsNoTracking().Where(PaymentRules.SuccessfulCharge)
            .Where(p => p.BookingId == booking.Id && p.PaymentKind == PaymentKind.Deposit).SumAsync(p => p.Amount, ct);
        return new(payment.Id, payment.TransactionStatus.ToString(), booking.Id, booking.BookingCode,
            booking.BookingStatus.ToString(), booking.PaymentStatus.ToString(), booking.CourtNameSnapshot,
            booking.CenterNameSnapshot, booking.SportNameSnapshot, booking.StartAt, booking.EndAt, booking.DurationMinutes,
            booking.TotalAmount, depositPaid, PaymentRules.CalculateRemaining(booking.TotalAmount, paid));
    }

    private static void ValidateHold(Booking booking, IReadOnlyList<BookingSlot> slots, DateTimeOffset now, string expiredCode)
    {
        if (booking.BookingStatus == BookingStatus.Expired ||
            (booking.BookingStatus == BookingStatus.PendingPayment && (booking.HoldExpiresAt == null || booking.HoldExpiresAt <= now)))
            throw new ConflictException("The booking hold has expired.", expiredCode);
        if (booking.BookingStatus != BookingStatus.PendingPayment || booking.PaymentStatus != BookingPaymentStatus.Unpaid)
            throw new ConflictException("Booking is not awaiting deposit payment.", ErrorCodes.BookingNotPendingPayment);
        if (slots.Count == 0)
            throw new ConflictException("Booking has no held slots.", ErrorCodes.PaymentStateConflict);
        if (slots.Any(s => s.HoldExpiresAt == null || s.HoldExpiresAt <= now || s.ReservationState == ReservationState.Released))
            throw new ConflictException("The booking slot hold has expired.", expiredCode);
        if (slots.Any(s => s.ReservationState != ReservationState.Held || !s.IsOccupying || s.CourtId != booking.CourtId))
            throw new ConflictException("Booking slots are no longer held.", ErrorCodes.PaymentStateConflict);
    }

    private static DepositPaymentResponse ToStartResponse(Booking booking, Payment payment, GatewayPaymentAttempt attempt)
        => new(payment.Id, booking.Id, booking.BookingCode, payment.PaymentKind.ToString(), payment.PaymentMethod.ToString(),
            payment.Amount, payment.TransactionStatus.ToString(), payment.ProviderTransactionId,
            attempt.PaymentUrl, booking.HoldExpiresAt, attempt.Gateway);
    private static PaymentMethod ParseMethod(string? value) => value?.ToLowerInvariant() switch
    {
        "momo" => PaymentMethod.MoMo, "vnpay" => PaymentMethod.VNPay,
        _ => throw new ValidationException("Supported development methods are MoMo and VNPay.", ErrorCodes.UnsupportedPaymentMethod)
    };
    private static void ValidateOwner(Booking booking, Guid customerUserId)
    {
        if (booking.CustomerUserId != customerUserId) throw BookingNotFound();
    }
    private static NotFoundException BookingNotFound() => new("Booking was not found.", ErrorCodes.BookingNotFound);
    private static NotFoundException PaymentNotFound() => new("Payment was not found.", ErrorCodes.PaymentNotFound);

    private static async Task<T> WithUniqueConflictHandlingAsync<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("Payment reference or booking state conflicts with an existing record.", ErrorCodes.PaymentStateConflict);
        }
    }
}
