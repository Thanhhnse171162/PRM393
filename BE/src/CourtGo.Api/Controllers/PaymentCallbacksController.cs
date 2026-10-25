using System.Text.Json;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using CourtGo.Infrastructure.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/payments")]
public sealed class PaymentCallbacksController(PaymentNotificationVerifier verifier, IDepositPaymentService deposits,
    CourtGoDbContext db, ILogger<PaymentCallbacksController> logger) : ControllerBase
{
    [HttpPost("momo/ipn"), RequestSizeLimit(16384)]
    public async Task<IActionResult> MoMoIpn([FromBody] JsonElement body, CancellationToken ct)
    {
        var result = verifier.VerifyMoMo(JsonFields(body));
        await ValidateRecord(result, ct);
        if (result.TransactionStatus != PaymentTransactionStatus.Pending)
            await deposits.ApplyVerifiedResultAsync(result, cancellationToken: ct);
        return NoContent();
    }

    [HttpGet("vnpay/ipn")]
    public async Task<IActionResult> VnPayIpn(CancellationToken ct)
    {
        VerifiedPaymentResult result;
        try { result = verifier.VerifyVnPay(QueryFields()); }
        catch (ValidationException) { logger.LogWarning("VNPAY IPN rejected: invalid signature or fields"); return Ack("97", "Invalid signature"); }
        try
        {
            var already = await ValidateRecord(result, ct);
            if (result.TransactionStatus == PaymentTransactionStatus.Pending) return Ack("99", "Transaction pending");
            await deposits.ApplyVerifiedResultAsync(result, cancellationToken: ct);
            logger.LogInformation("VNPAY IPN applied PaymentId={PaymentId} Status={Status} Receipt={Receipt} Duplicate={Duplicate}",
                result.PaymentId, result.TransactionStatus, result.ProviderReceiptId, already);
            return Ack(already ? "02" : "00", already ? "Order already confirmed" : "Confirm success");
        }
        catch (NotFoundException) { return Ack("01", "Order not found"); }
        catch (ConflictException ex) when (ex.Code == ErrorCodes.PaymentAmountMismatch) { return Ack("04", "Invalid amount"); }
        catch (ConflictException) { return Ack("99", "Reconciliation required"); }
        catch (ValidationException) { return Ack("97", "Invalid payment"); }
    }

    // A browser return is only a navigation hint; it NEVER calls the finalization pipeline.
    [HttpGet("momo/return")]
    public Task<IActionResult> MoMoReturn(CancellationToken ct) => ReturnToApp(verifier.VerifyMoMo(QueryFields()), ct);
    [HttpGet("vnpay/return")]
    public Task<IActionResult> VnPayReturn(CancellationToken ct) => ReturnToApp(verifier.VerifyVnPay(QueryFields()), ct);

    private async Task<IActionResult> ReturnToApp(VerifiedPaymentResult result, CancellationToken ct)
    {
        await ValidateRecord(result, ct);
        var bookingId = await db.Payments.Where(p => p.Id == result.PaymentId).Select(p => p.BookingId).SingleAsync(ct);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
        logger.LogInformation("Payment return verified PaymentId={PaymentId} BookingId={BookingId}; navigation only", result.PaymentId, bookingId);
        // An explicit browser gesture opens Android custom schemes reliably, including when automatic redirects are blocked.
        // Only a DB-derived GUID is interpolated; no provider message/query is reflected or treated as payment proof.
        return Content($$"""
            <!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>Trở về CourtGo</title><style>body{font:18px system-ui;max-width:440px;margin:15vh auto;padding:24px;color:#183c36}a{display:block;background:#087f6f;color:white;padding:16px;border-radius:12px;text-align:center;text-decoration:none}p{line-height:1.6}</style>
            <h1>Trở về CourtGo</h1><p>Mở ứng dụng để hệ thống kiểm tra kết quả thanh toán và trạng thái lịch đặt.</p>
            <a href="courtgo://payment-result?bookingId={{bookingId:D}}">Mở CourtGo</a>
            <p>Nếu ứng dụng chưa mở, vào CourtGo → Lịch đặt → Chi tiết → Kiểm tra kết quả thanh toán. Không cần thanh toán lại.</p></html>
            """, "text/html; charset=utf-8");
    }
    private async Task<bool> ValidateRecord(VerifiedPaymentResult result, CancellationToken ct)
    {
        var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == result.PaymentId, ct)
            ?? throw new NotFoundException("Payment not found.", ErrorCodes.PaymentNotFound);
        if (payment.PaymentKind != PaymentKind.Deposit || payment.PaymentMethod.ToString() != result.Gateway ||
            payment.ProviderTransactionId != result.ProviderTransactionId)
            throw new ValidationException("Payment does not match provider.", ErrorCodes.PaymentVerificationFailed);
        if (payment.Amount != result.Amount) throw new ConflictException("Payment amount does not match.", ErrorCodes.PaymentAmountMismatch);
        return payment.TransactionStatus != PaymentTransactionStatus.Pending;
    }
    private Dictionary<string, string> QueryFields()
    {
        if (Request.Query.Any(p => p.Value.Count != 1)) throw new ValidationException("Duplicate callback field.", ErrorCodes.PaymentVerificationFailed);
        return Request.Query.ToDictionary(p => p.Key, p => p.Value.ToString(), StringComparer.Ordinal);
    }
    private static Dictionary<string, string> JsonFields(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object) throw new ValidationException("Invalid notification.");
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in body.EnumerateObject())
        {
            if (property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number) ||
                !fields.TryAdd(property.Name, property.Value.ToString())) throw new ValidationException("Invalid notification field.");
        }
        return fields;
    }
    private OkObjectResult Ack(string code, string message) => Ok(new Dictionary<string, string>
        { ["RspCode"] = code, ["Message"] = message });
}
