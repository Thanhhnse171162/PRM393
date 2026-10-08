using System.Security.Claims;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize(Roles = "Customer")]
public class BookingPaymentsController(IDepositPaymentService depositPaymentService) : ControllerBase
{
    private readonly IDepositPaymentService _depositPaymentService = depositPaymentService;

    [HttpPost("{bookingId:guid}/payments/deposit")]
    public async Task<ActionResult<DepositPaymentResponse>> StartDepositAsync(Guid bookingId,
        [FromBody] StartDepositPaymentRequest request, CancellationToken cancellationToken)
        => Ok(await _depositPaymentService.StartDepositAsync(GetUserId(), bookingId, request, cancellationToken));

    [HttpGet("{bookingId:guid}/payment-status")]
    public async Task<ActionResult<BookingPaymentStatusResponse>> GetPaymentStatusAsync(Guid bookingId, CancellationToken cancellationToken)
        => Ok(await _depositPaymentService.GetPaymentStatusAsync(GetUserId(), bookingId, cancellationToken));

    private Guid GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedException("User identity could not be verified from token.");
    }
}
