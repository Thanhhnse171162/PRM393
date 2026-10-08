using System.Linq.Expressions;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Operations;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public partial class CancellationService(CourtGoDbContext db, BookingCommandExecutor commands, TimeProvider clock) : ICancellationService
{
    private static readonly Expression<Func<CancellationRequest, CancellationRequestDto>> Projection = r =>
        new(r.Id, r.BookingId, r.Status.ToString(), r.Reason, r.CalculatedRefundAmount, r.ApprovedRefundAmount, r.CreatedAt, r.ProcessedAt);

    private static CancellationRequestDto ToDto(CancellationRequest r) =>
        new(r.Id, r.BookingId, r.Status.ToString(), r.Reason, r.CalculatedRefundAmount, r.ApprovedRefundAmount, r.CreatedAt, r.ProcessedAt);

    public Task<CancellationRequestDto> CreateRequestAsync(Guid customerId, Guid bookingId, CreateCancellationRequest request, CancellationToken ct)
        => commands.ExecuteAsync(bookingId, async (booking, token) =>
        {
            if (booking.CustomerUserId != customerId) throw new NotFoundException("Booking not found.", ErrorCodes.BookingNotFound);
            if (booking.BookingStatus != BookingStatus.Confirmed) throw new ConflictException("Only confirmed bookings can request cancellation.");
            ValidateReason(request.Reason);
            var pending = await db.CancellationRequests.AsNoTracking().SingleOrDefaultAsync(r => r.BookingId == bookingId && r.Status == CancellationRequestStatus.Pending, token);
            if (pending is not null) return ToDto(pending);
            var now = clock.GetUtcNow();
            decimal percent = 0;
            if (booking.CancellationPolicyId is Guid policyId)
            {
                if (!await db.CancellationPolicies.AnyAsync(p => p.Id == policyId, token))
                    throw new ConfigurationException("Booking cancellation policy is missing.");
                var zoneId = await db.Courts.Where(c => c.Id == booking.CourtId).Select(c => c.SportCenter!.TimeZoneId).SingleAsync(token);
                var zone = TimeZoneHelper.ResolveTimeZone(zoneId);
                var hours = (TimeZoneInfo.ConvertTime(booking.StartAt, zone) - TimeZoneInfo.ConvertTime(now, zone)).TotalHours;
                var rules = await db.CancellationPolicyRules.AsNoTracking().Where(r => r.CancellationPolicyId == policyId).ToListAsync(token);
                var matching = rules.Where(r => hours >= r.MinHoursBeforeStart && (r.MaxHoursBeforeStart == null || hours < r.MaxHoursBeforeStart)).ToList();
                if (matching.Count > 1 || matching.Any(r => r.RefundPercent is < 0 or > 100))
                    throw new ConfigurationException("Invalid cancellation policy rules.");
                percent = matching.SingleOrDefault()?.RefundPercent ?? 0;
            }
            var refundable = await RefundableAsync(bookingId, token);
            var created = new CancellationRequest
            {
                BookingId = bookingId,
                RequestedByUserId = customerId,
                Reason = request.Reason.Trim(),
                Status = CancellationRequestStatus.Pending,
                CreatedAt = now,
                CalculatedRefundAmount = Math.Min(refundable, decimal.Round(refundable * percent / 100m, 0, MidpointRounding.AwayFromZero))
            };
            db.CancellationRequests.Add(created);
            return ToDto(created);
        }, ct);

    public async Task<PagedResult<CancellationRequestDto>> GetRequestsAsync(Guid customerId, Guid bookingId, int pageNumber, int pageSize, CancellationToken ct)
    {
        if (!await db.Bookings.AnyAsync(b => b.Id == bookingId && b.CustomerUserId == customerId, ct))
            throw new NotFoundException("Booking not found.", ErrorCodes.BookingNotFound);
        StaffOperationsService.ValidatePage(pageNumber, pageSize);
        var query = db.CancellationRequests.AsNoTracking().Where(r => r.BookingId == bookingId);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id).Skip((pageNumber - 1) * pageSize)
            .Take(pageSize).Select(Projection).ToListAsync(ct);
        return new(items, pageNumber, pageSize, count, (int)Math.Ceiling(count / (double)pageSize));
    }

    public async Task<PagedResult<AdminCancellationRequestSummaryDto>> GetAdminRequestsAsync(AdminCancellationRequestQuery query, CancellationToken ct = default)
    {
        StaffOperationsService.ValidatePage(query.PageNumber, query.PageSize);
        var requests = db.CancellationRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<CancellationRequestStatus>(query.Status, true, out var status) || !Enum.IsDefined(status))
                throw new ValidationException("Invalid cancellation request status.");
            requests = requests.Where(r => r.Status == status);
        }

        if (query.CenterId is Guid centerId)
        {
            requests = requests.Where(r => r.Booking!.Court!.SportCenterId == centerId);
        }

        if (query.DateFrom is DateTimeOffset dateFrom)
        {
            requests = requests.Where(r => r.CreatedAt >= dateFrom);
        }

        if (query.DateTo is DateTimeOffset dateTo)
        {
            requests = requests.Where(r => r.CreatedAt <= dateTo);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (search.Length > 200) throw new ValidationException("Search exceeds 200 characters.");
            requests = requests.Where(r =>
                r.Booking!.BookingCode.Contains(search) ||
                r.Booking.CustomerNameSnapshot.Contains(search) ||
                r.Booking.CustomerPhoneSnapshot.Contains(search));
        }

        var total = await requests.CountAsync(ct);
        var items = await requests
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new AdminCancellationRequestSummaryDto(
                r.Id,
                r.BookingId,
                r.Booking!.BookingCode,
                r.Status.ToString(),
                r.Reason,
                r.CalculatedRefundAmount,
                r.ApprovedRefundAmount,
                r.Booking.CustomerNameSnapshot,
                r.Booking.CustomerPhoneSnapshot,
                r.Booking.CenterNameSnapshot,
                r.Booking.CourtNameSnapshot,
                r.Booking.SportNameSnapshot,
                r.Booking.StartAt,
                r.Booking.EndAt,
                r.CreatedAt,
                r.ProcessedAt))
            .ToListAsync(ct);

        return new(items, query.PageNumber, query.PageSize, total, (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public async Task<AdminCancellationRequestDetailDto> GetAdminRequestDetailAsync(Guid requestId, CancellationToken ct = default)
    {
        var request = await db.CancellationRequests.AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => new
            {
                r.Id,
                r.Status,
                r.Reason,
                r.CreatedAt,
                r.ProcessedAt,
                r.ProcessedByUserId,
                r.CalculatedRefundAmount,
                r.ApprovedRefundAmount,
                Booking = new
                {
                    r.Booking!.Id,
                    r.Booking.BookingCode,
                    r.Booking.BookingStatus,
                    r.Booking.CustomerUserId,
                    r.Booking.CustomerNameSnapshot,
                    r.Booking.CustomerPhoneSnapshot,
                    r.Booking.CustomerEmailSnapshot,
                    r.Booking.CenterNameSnapshot,
                    r.Booking.CourtNameSnapshot,
                    r.Booking.SportNameSnapshot,
                    r.Booking.StartAt,
                    r.Booking.EndAt,
                    r.Booking.TotalAmount,
                    r.Booking.PaymentStatus,
                    r.Booking.CancellationPolicyId,
                    r.Booking.CourtId
                }
            })
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Cancellation request not found.");

        var payments = await db.Payments.AsNoTracking()
            .Where(p => p.BookingId == request.Booking.Id)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Select(p => new PaymentItemDto(
                p.Id,
                p.PaymentKind.ToString(),
                p.PaymentMethod.ToString(),
                p.Amount,
                p.TransactionStatus.ToString(),
                p.PaidAt,
                p.CreatedAt))
            .ToListAsync(ct);

        var paidAmount = payments
            .Where(p => (p.PaymentKind == PaymentKind.Deposit.ToString() || p.PaymentKind == PaymentKind.Remaining.ToString()) &&
                        p.TransactionStatus == PaymentTransactionStatus.Succeeded.ToString())
            .Sum(p => p.Amount);

        var refundedAmount = payments
            .Where(p => p.PaymentKind == PaymentKind.Refund.ToString() &&
                        p.TransactionStatus == PaymentTransactionStatus.Succeeded.ToString())
            .Sum(p => p.Amount);

        CancellationPolicySnapshotDto? policyDto = null;
        if (request.Booking.CancellationPolicyId is Guid policyId)
        {
            var policy = await db.CancellationPolicies.AsNoTracking()
                .Where(p => p.Id == policyId)
                .Select(p => new { p.Name })
                .SingleOrDefaultAsync(ct);

            decimal? percent = null;
            var courtZone = await db.Courts.Where(c => c.Id == request.Booking.CourtId)
                .Select(c => c.SportCenter!.TimeZoneId).SingleOrDefaultAsync(ct);
            if (courtZone is not null)
            {
                var zone = TimeZoneHelper.ResolveTimeZone(courtZone);
                var hours = (TimeZoneInfo.ConvertTime(request.Booking.StartAt, zone) - TimeZoneInfo.ConvertTime(request.CreatedAt, zone)).TotalHours;
                var rules = await db.CancellationPolicyRules.AsNoTracking().Where(r => r.CancellationPolicyId == policyId).ToListAsync(ct);
                var matching = rules.Where(r => hours >= r.MinHoursBeforeStart && (r.MaxHoursBeforeStart == null || hours < r.MaxHoursBeforeStart)).ToList();
                if (matching.Count == 1) percent = matching[0].RefundPercent;
            }

            policyDto = new CancellationPolicySnapshotDto(policyId, policy?.Name, percent);
        }

        return new AdminCancellationRequestDetailDto(
            request.Id,
            request.Status.ToString(),
            request.Reason,
            request.CreatedAt,
            request.ProcessedAt,
            request.ProcessedByUserId,
            request.Booking.Id,
            request.Booking.BookingCode,
            request.Booking.BookingStatus.ToString(),
            new CustomerSnapshotDto(
                request.Booking.CustomerUserId,
                request.Booking.CustomerNameSnapshot,
                request.Booking.CustomerPhoneSnapshot,
                request.Booking.CustomerEmailSnapshot),
            new FacilitySnapshotDto(
                request.Booking.CenterNameSnapshot,
                request.Booking.CourtNameSnapshot,
                request.Booking.SportNameSnapshot),
            request.Booking.StartAt,
            request.Booking.EndAt,
            request.Booking.TotalAmount,
            paidAmount,
            request.CalculatedRefundAmount,
            request.ApprovedRefundAmount,
            policyDto,
            new PaymentSummaryDto(
                request.Booking.TotalAmount,
                paidAmount,
                refundedAmount,
                request.Booking.PaymentStatus.ToString(),
                payments));
    }

    public async Task<CancellationDecisionResponse> ProcessDecisionAsync(Guid adminUserId, Guid requestId, CancellationDecisionRequest request, CancellationToken ct = default)
    {
        var normalizedDecision = request.Decision?.Trim().ToLowerInvariant();
        if (normalizedDecision is not ("approve" or "reject"))
            throw new ValidationException("Decision must be 'approve' or 'reject'.");

        var targetReq = await db.CancellationRequests.AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => new { r.Id, r.BookingId, r.Status })
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Cancellation request not found.");

        return await commands.ExecuteAsync(targetReq.BookingId, async (booking, token) =>
        {
            var cr = await db.CancellationRequests.SingleOrDefaultAsync(r => r.Id == requestId, token)
                ?? throw new NotFoundException("Cancellation request not found.");

            var now = clock.GetUtcNow();

            if (cr.Status != CancellationRequestStatus.Pending)
            {
                if (normalizedDecision == "approve" && cr.Status == CancellationRequestStatus.Approved)
                {
                    var existingRefund = await db.Payments.AsNoTracking()
                        .Where(p => p.BookingId == booking.Id && p.PaymentKind == PaymentKind.Refund)
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefaultAsync(token);
                    return new CancellationDecisionResponse(
                        cr.Id,
                        booking.Id,
                        "approve",
                        cr.Status.ToString(),
                        booking.BookingStatus.ToString(),
                        cr.ApprovedRefundAmount,
                        existingRefund?.Id,
                        cr.ProcessedAt ?? now);
                }
                if (normalizedDecision == "reject" && cr.Status == CancellationRequestStatus.Rejected)
                {
                    return new CancellationDecisionResponse(
                        cr.Id,
                        booking.Id,
                        "reject",
                        cr.Status.ToString(),
                        booking.BookingStatus.ToString(),
                        null,
                        null,
                        cr.ProcessedAt ?? now);
                }
                throw new ConflictException("Cancellation request has already been processed.");
            }

            if (normalizedDecision == "reject")
            {
                cr.Status = CancellationRequestStatus.Rejected;
                cr.ProcessedByUserId = adminUserId;
                cr.ProcessedAt = now;

                if (booking.CustomerUserId is Guid customerId)
                {
                    var rejectNote = !string.IsNullOrWhiteSpace(request.Reason) ? $" Lý do: {request.Reason.Trim()}" : "";
                    db.Notifications.Add(new Notification
                    {
                        UserId = customerId,
                        Type = NotificationType.Booking,
                        Title = "Yêu cầu hủy đặt sân bị từ chối",
                        Message = $"Yêu cầu hủy lịch đặt {booking.BookingCode} đã bị từ chối.{rejectNote}",
                        ReferenceType = "Booking",
                        ReferenceId = booking.Id,
                        CreatedAt = now
                    });
                }

                return new CancellationDecisionResponse(
                    cr.Id,
                    booking.Id,
                    "reject",
                    cr.Status.ToString(),
                    booking.BookingStatus.ToString(),
                    null,
                    null,
                    now);
            }

            // Decision == "approve"
            if (booking.BookingStatus != BookingStatus.Confirmed)
                throw new ConflictException("Only confirmed bookings can be cancelled.");

            var approvedRefund = request.ApprovedRefundAmount ?? cr.CalculatedRefundAmount;
            if (approvedRefund < 0)
                throw new ValidationException("Approved refund amount must be non-negative.");
            if (approvedRefund > cr.CalculatedRefundAmount)
                throw new ValidationException("Approved refund amount cannot exceed calculated refund amount.");

            var refundable = await RefundableAsync(booking.Id, token);
            if (approvedRefund > refundable)
                throw new ValidationException("Approved refund amount cannot exceed actual refundable charges.");

            cr.Status = CancellationRequestStatus.Approved;
            cr.ApprovedRefundAmount = approvedRefund;
            cr.ProcessedByUserId = adminUserId;
            cr.ProcessedAt = now;

            booking.BookingStatus = BookingStatus.Cancelled;

            var slots = await commands.ReloadSlotsAsync(booking.Id, token);
            foreach (var slot in slots)
            {
                slot.ReservationState = ReservationState.Released;
                slot.IsOccupying = false;
                slot.HoldExpiresAt = null;
            }

            var reason = !string.IsNullOrWhiteSpace(request.Reason)
                ? request.Reason.Trim()
                : "Cancellation request approved";

            db.BookingStatusHistories.Add(new BookingStatusHistory
            {
                BookingId = booking.Id,
                FromStatus = BookingStatus.Confirmed,
                ToStatus = BookingStatus.Cancelled,
                ChangedByUserId = adminUserId,
                Reason = reason,
                CreatedAt = now
            });

            Payment? refundPayment = null;
            if (approvedRefund > 0)
            {
                var originalPayment = await db.Payments.AsNoTracking()
                    .Where(p => p.BookingId == booking.Id && (p.PaymentKind == PaymentKind.Deposit || p.PaymentKind == PaymentKind.Remaining) && p.TransactionStatus == PaymentTransactionStatus.Succeeded)
                    .OrderBy(p => p.CreatedAt)
                    .FirstOrDefaultAsync(token);

                var method = originalPayment?.PaymentMethod ?? PaymentMethod.MoMo;

                refundPayment = new Payment
                {
                    BookingId = booking.Id,
                    PaymentKind = PaymentKind.Refund,
                    PaymentMethod = method,
                    Amount = approvedRefund,
                    TransactionStatus = PaymentTransactionStatus.Pending,
                    RelatedPaymentId = originalPayment?.Id,
                    PaidByUserId = adminUserId,
                    CreatedAt = now
                };
                refundPayment.ProviderTransactionId = $"dev-{method}-{refundPayment.Id:N}";
                db.Payments.Add(refundPayment);

                booking.PaymentStatus = BookingPaymentStatus.RefundPending;
            }

            if (booking.CustomerUserId is Guid recipient)
            {
                var refundText = approvedRefund > 0 ? $" Số tiền hoàn dự kiến: {approvedRefund:N0} đ." : "";
                db.Notifications.Add(new Notification
                {
                    UserId = recipient,
                    Type = NotificationType.Booking,
                    Title = "Yêu cầu hủy đặt sân được chấp thuận",
                    Message = $"Yêu cầu hủy lịch đặt {booking.BookingCode} đã được chấp thuận.{refundText}",
                    ReferenceType = "Booking",
                    ReferenceId = booking.Id,
                    CreatedAt = now
                });
            }

            return new CancellationDecisionResponse(
                cr.Id,
                booking.Id,
                "approve",
                cr.Status.ToString(),
                booking.BookingStatus.ToString(),
                approvedRefund,
                refundPayment?.Id,
                now);
        }, ct);
    }

    private async Task<decimal> RefundableAsync(Guid bookingId, CancellationToken ct)
    {
        var paid = await BookingPaymentQueries.GetPaidAmountAsync(db, bookingId, ct);
        var refunded = await db.Payments.Where(p => p.BookingId == bookingId && p.PaymentKind == PaymentKind.Refund &&
            (p.TransactionStatus == PaymentTransactionStatus.Succeeded || p.TransactionStatus == PaymentTransactionStatus.Pending))
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0;
        return Math.Max(0, paid - refunded);
    }

    private static void ValidateReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500) throw new ValidationException("Reason is required, maximum 500 characters.");
    }
}
