using CourtGo.Api.Payments;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(CourtGoDbContext db, VnPayGateway gateway) : ControllerBase
{
    [Authorize(Policy = "CustomerOnly")]
    [HttpPost("vnpay/create")]
    public async Task<IActionResult> CreateVnPay([FromBody] CreateVnPayRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var customerId)) return Unauthorized();

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var booking = await db.Bookings.SingleOrDefaultAsync(b => b.Id == request.BookingId && b.CustomerId == customerId, ct);
        if (booking is null) return NotFound();
        if (booking.Status != BookingStatus.PendingPayment || booking.PaymentStatus != PaymentStatus.Unpaid ||
            booking.HoldExpiresAt is not null && booking.HoldExpiresAt <= DateTime.UtcNow)
            return Conflict(new ProblemDetails { Status = 409, Title = "Booking is not payable" });
        if (booking.DepositAmount <= 0 || booking.DepositAmount != decimal.Truncate(booking.DepositAmount))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid deposit amount" });

        var payment = await db.Payments
            .Where(p => p.BookingId == booking.Id && p.Method == PaymentMethod.VNPay &&
                        p.Type == PaymentType.Deposit && p.Status == PaymentStatus.Unpaid)
            .OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync(ct);
        if (payment is not null && payment.CreatedAt.AddMinutes(15) <= DateTime.UtcNow)
        {
            payment.Status = PaymentStatus.Failed;
            payment = null;
        }
        if (payment is null)
        {
            payment = new Payment
            {
                BookingId = booking.Id,
                Amount = booking.DepositAmount,
                Type = PaymentType.Deposit,
                Method = PaymentMethod.VNPay
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync(ct);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
        var url = gateway.CreatePaymentUrl(payment.Id, payment.Amount, ip, payment.CreatedAt);
        await transaction.CommitAsync(ct);
        return Ok(new { paymentId = payment.Id, bookingId = booking.Id, amount = payment.Amount, paymentUrl = url });
    }

    [Authorize(Policy = "CustomerOnly")]
    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetPayment(Guid paymentId, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var customerId)) return Unauthorized();
        var payment = await db.Payments.Include(p => p.Booking)
            .SingleOrDefaultAsync(p => p.Id == paymentId && p.Booking!.CustomerId == customerId, ct);
        if (payment is null) return NotFound();
        return Ok(new { paymentId = payment.Id, bookingId = payment.BookingId, amount = payment.Amount,
            status = payment.Status.ToString(), bookingStatus = payment.Booking!.Status.ToString(), paidAt = payment.PaidAt });
    }

    [AllowAnonymous]
    [HttpGet("vnpay/return")]
    public async Task<IActionResult> VnPayReturn(CancellationToken ct)
    {
        if (!gateway.TryValidate(Request.Query, out var result)) return BadRequest("Invalid VNPay signature or payload.");
        var code = await ApplyResultAsync(result, ct);
        if (!string.IsNullOrWhiteSpace(gateway.FrontendReturnUrl) &&
            Uri.TryCreate(gateway.FrontendReturnUrl, UriKind.Absolute, out var destination))
        {
            var separator = destination.Query.Length > 0 ? "&" : "?";
            return Redirect($"{destination}{separator}paymentId={result.PaymentId:N}");
        }
        return Ok(new { paymentId = result.PaymentId, result = code,
            message = "Open the app and refresh the payment status." });
    }

    [AllowAnonymous]
    [HttpGet("vnpay/ipn")]
    public async Task<IActionResult> VnPayIpn(CancellationToken ct)
    {
        if (!gateway.TryValidate(Request.Query, out var result))
            return Ok(new { RspCode = "97", Message = "Invalid checksum" });
        var code = await ApplyResultAsync(result, ct);
        return Ok(new { RspCode = code, Message = code switch
        {
            "00" => "Confirm Success", "01" => "Order not found", "02" => "Order already confirmed",
            "04" => "Invalid amount", _ => "Invalid order"
        } });
    }

    private async Task<string> ApplyResultAsync(VnPayResult result, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var payment = await db.Payments.Include(p => p.Booking)
            .SingleOrDefaultAsync(p => p.Id == result.PaymentId && p.Method == PaymentMethod.VNPay, ct);
        if (payment is null) return "01";
        if (payment.Amount * 100 != result.Amount) return "04";
        if (payment.Status == PaymentStatus.Failed && result.IsSuccessful)
        {
            // An old attempt can arrive after a retry. Record the money for manual refund
            // instead of confirming a booking that may already have another payment.
            payment.Status = PaymentStatus.RefundPending;
            payment.TransactionReference = result.TransactionNumber;
            payment.PaidAt = DateTime.UtcNow;
            if (payment.Booking!.Status == BookingStatus.PendingPayment)
                payment.Booking.PaymentStatus = PaymentStatus.RefundPending;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return "00";
        }
        if (payment.Status != PaymentStatus.Unpaid) return "02";
        var booking = payment.Booking!;
        if (result.IsSuccessful)
        {
            payment.TransactionReference = result.TransactionNumber;
            payment.PaidAt = DateTime.UtcNow;
            if (booking.Status == BookingStatus.PendingPayment &&
                (booking.HoldExpiresAt is null || booking.HoldExpiresAt > DateTime.UtcNow) &&
                booking.PaymentStatus == PaymentStatus.Unpaid)
            {
                payment.Status = PaymentStatus.DepositPaid;
                booking.PaymentStatus = PaymentStatus.DepositPaid;
                booking.Status = BookingStatus.Confirmed;
            }
            else
            {
                payment.Status = PaymentStatus.RefundPending;
                booking.PaymentStatus = PaymentStatus.RefundPending;
                if (booking.Status == BookingStatus.PendingPayment && booking.HoldExpiresAt <= DateTime.UtcNow)
                    booking.Status = BookingStatus.Expired;
            }
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return "00";
    }
}

public sealed record CreateVnPayRequest(Guid BookingId);
