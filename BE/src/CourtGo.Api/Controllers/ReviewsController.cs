using System.Security.Claims;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/bookings/{bookingId:guid}/reviews")]
[Authorize(Roles = "Customer")]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    private Guid CustomerUserId => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<ReviewDto>> CreateReviewAsync(
        Guid bookingId,
        [FromBody] CreateReviewRequest request,
        CancellationToken ct)
    {
        return Ok(await reviewService.CreateReviewAsync(CustomerUserId, bookingId, request, ct));
    }
}
