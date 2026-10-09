using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Operations;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Domain.Rules;
using Microsoft.EntityFrameworkCore;
namespace CourtGo.Infrastructure.Services;
public partial class CancellationService
{
    public async Task<PagedResult<CancellationRequestDto>> GetAdminRequestsAsync(string? status, int pageNumber, int pageSize, CancellationToken ct)
    {
        StaffOperationsService.ValidatePage(pageNumber, pageSize);
        var query = db.CancellationRequests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<CancellationRequestStatus>(status, true, out var value) || !Enum.IsDefined(value))
                throw new ValidationException("Invalid cancellation status.");
            query = query.Where(r => r.Status == value);
        }
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(Projection).ToListAsync(ct);
        return new(items, pageNumber, pageSize, count, (int)Math.Ceiling(count / (double)pageSize));
    }
    public async Task<CancellationDetailDto> GetAdminRequestAsync(Guid requestId, CancellationToken ct)
    {
        var request = await db.CancellationRequests.AsNoTracking().Where(r => r.Id == requestId).Select(Projection).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Cancellation request not found.");
        var refunds = await db.Payments.AsNoTracking().Where(p => p.BookingId == request.BookingId && p.PaymentKind == PaymentKind.Refund)
            .OrderBy(p => p.CreatedAt).ThenBy(p => p.Id).Select(p => new PaymentEntryDto(p.Id, p.PaymentKind.ToString(), p.PaymentMethod.ToString(),
                p.Amount, p.TransactionStatus.ToString(), p.PaidAt)).ToListAsync(ct);
        return new(request, refunds);
    }
    public async Task<CancellationRequestDto> DecideAsync(Guid adminId, Guid requestId, CancellationDecisionRequest request, CancellationToken ct)
    {
        ValidateReason(request.Reason);
        var approve = string.Equals(request.Decision, "approve", StringComparison.OrdinalIgnoreCase);
        if (!approve && !string.Equals(request.Decision, "reject", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Decision must be approve or reject.");
        if (approve && (request.ApprovedRefundAmount is null or < 0))
            throw new ValidationException("approvedRefundAmount is required and must be nonnegative.");
        var bookingId = await db.CancellationRequests.Where(r => r.Id == requestId).Select(r => (Guid?)r.BookingId).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Cancellation request not found.");
        return await commands.ExecuteAsync(bookingId, async (booking, token) =>
        {
            if (!await db.Users.AnyAsync(u => u.Id == adminId && u.IsActive && u.Role == UserRole.Admin, token))
                throw new ForbiddenException();
            var row = await db.CancellationRequests.SingleAsync(r => r.Id == requestId, token);
            await db.Entry(row).ReloadAsync(token);
            if (row.Status != CancellationRequestStatus.Pending)
            {
                if ((!approve && row.Status == CancellationRequestStatus.Rejected) ||
                    (approve && row.Status != CancellationRequestStatus.Rejected && row.ApprovedRefundAmount == request.ApprovedRefundAmount))
                    return ToDto(row);
                throw new ConflictException("Cancellation has already been decided.");
            }
            var now = clock.GetUtcNow();
            if (approve)
            {
                if (booking.BookingStatus != BookingStatus.Confirmed) throw new ConflictException("Booking is no longer cancellable.");
                var amount = request.ApprovedRefundAmount!.Value;
                if (decimal.Round(amount, 2) != amount || amount > row.CalculatedRefundAmount || amount > await RefundableAsync(bookingId, token))
                    throw new ValidationException("Refund exceeds policy allowance or available successful payments.");
                var charges = await db.Payments.Where(p => p.BookingId == bookingId).Where(PaymentRules.SuccessfulCharge)
                    .OrderBy(p => p.CreatedAt).ThenBy(p => p.Id).ToListAsync(token);
                var existingRefunds = await db.Payments.AsNoTracking().Where(p => p.BookingId == bookingId && p.PaymentKind == PaymentKind.Refund &&
                    (p.TransactionStatus == PaymentTransactionStatus.Pending || p.TransactionStatus == PaymentTransactionStatus.Succeeded)).ToListAsync(token);
                var left = amount;
                foreach (var charge in charges)
                {
                    var part = Math.Min(left, Math.Max(0, charge.Amount - existingRefunds.Where(p => p.RelatedPaymentId == charge.Id).Sum(p => p.Amount)));
                    if (part <= 0) continue;
                    var refund = new Payment { BookingId = bookingId, PaymentKind = PaymentKind.Refund, PaymentMethod = charge.PaymentMethod,
                        Amount = part, RelatedPaymentId = charge.Id, TransactionStatus = PaymentTransactionStatus.Pending,
                        ConfirmedByUserId = adminId, CreatedAt = now };
                    var attempt = await gateway.CreateRefundAsync(new(refund.Id, bookingId, refund.PaymentMethod, part), charge.Id, token);
                    refund.ProviderTransactionId = attempt.ProviderTransactionId;
                    db.Payments.Add(refund);
                    left -= part;
                    if (left == 0) break;
                }
                if (left > 0) throw new ConflictException("Successful original charges cannot cover refund.");
                row.ApprovedRefundAmount = amount;
                row.Status = amount > 0 ? CancellationRequestStatus.RefundPending : CancellationRequestStatus.Approved;
                if (amount > 0) booking.PaymentStatus = BookingPaymentStatus.RefundPending;
                var slots = await commands.ReloadSlotsAsync(bookingId, token);
                foreach (var slot in slots.Where(s => s.ReservationState == ReservationState.Reserved))
                { slot.ReservationState = ReservationState.Released; slot.IsOccupying = false; slot.HoldExpiresAt = null; }
                booking.BookingStatus = BookingStatus.Cancelled;
            }
            else row.Status = CancellationRequestStatus.Rejected;
            row.ProcessedByUserId = adminId;
            row.ProcessedAt = now;
            // The existing schema has no decision-reason column; history retains the administrator's reason.
            db.BookingStatusHistories.Add(new BookingStatusHistory { BookingId = bookingId,
                FromStatus = approve ? BookingStatus.Confirmed : booking.BookingStatus, ToStatus = booking.BookingStatus,
                ChangedByUserId = adminId, Reason = request.Reason!.Trim(), CreatedAt = now });
            Notify(booking, NotificationType.Cancellation, approve ? "Cancellation approved" : "Cancellation rejected", request.Reason!.Trim(), now);
            return ToDto(row);
        }, ct);
    }
    public async Task<CancellationRequestDto> ApplyVerifiedRefundAsync(VerifiedPaymentResult result, CancellationToken ct)
    {
        var bookingId = await db.Payments.Where(p => p.Id == result.PaymentId && p.PaymentKind == PaymentKind.Refund)
            .Select(p => (Guid?)p.BookingId).SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Refund not found.");
        return await commands.ExecuteAsync(bookingId, async (booking, token) =>
        {
            var payment = await db.Payments.SingleAsync(p => p.Id == result.PaymentId, token);
            await db.Entry(payment).ReloadAsync(token);
            if (payment.ProviderTransactionId != result.ProviderTransactionId || payment.Amount != result.Amount ||
                result.TransactionStatus != PaymentTransactionStatus.Succeeded)
                throw new ValidationException("Refund verification failed.", ErrorCodes.PaymentVerificationFailed);
            var request = await db.CancellationRequests.SingleOrDefaultAsync(r => r.BookingId == bookingId &&
                (r.Status == CancellationRequestStatus.RefundPending || r.Status == CancellationRequestStatus.Completed), token)
                ?? throw new ConflictException("No approved refund request exists.");
            await db.Entry(request).ReloadAsync(token);
            if (payment.TransactionStatus == PaymentTransactionStatus.Succeeded) return ToDto(request);
            if (payment.TransactionStatus != PaymentTransactionStatus.Pending || booking.BookingStatus != BookingStatus.Cancelled)
                throw new ConflictException("Refund is not pending.");
            var now = clock.GetUtcNow();
            payment.TransactionStatus = PaymentTransactionStatus.Succeeded;
            payment.PaidAt = now;
            await db.SaveChangesAsync(token);
            var refunded = await db.Payments.Where(p => p.BookingId == bookingId && p.PaymentKind == PaymentKind.Refund &&
                p.TransactionStatus == PaymentTransactionStatus.Succeeded).SumAsync(p => p.Amount, token);
            if (!await db.Payments.AnyAsync(p => p.BookingId == bookingId && p.PaymentKind == PaymentKind.Refund && p.TransactionStatus == PaymentTransactionStatus.Pending, token))
            {
                request.Status = CancellationRequestStatus.Completed;
                var paid = await BookingPaymentQueries.GetPaidAmountAsync(db, bookingId, token);
                booking.PaymentStatus = refunded >= paid ? BookingPaymentStatus.Refunded : BookingPaymentStatus.PartiallyRefunded;
                Notify(booking, NotificationType.Refund, "Refund completed", $"Refund completed for {booking.BookingCode}.", now);
            }
            return ToDto(request);
        }, ct);
    }
    private void Notify(Booking booking, NotificationType type, string title, string message, DateTimeOffset now)
    {
        if (booking.CustomerUserId is Guid customer)
            db.Notifications.Add(new Notification { UserId = customer, Type = type, Title = title, Message = message,
                ReferenceType = "Booking", ReferenceId = booking.Id, CreatedAt = now });
    }
}
