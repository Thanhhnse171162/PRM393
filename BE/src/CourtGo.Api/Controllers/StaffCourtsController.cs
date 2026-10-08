using System.Security.Claims;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CourtGo.Api.Controllers;
[ApiController, Route("api/staff/courts"), Authorize(Roles = "Staff")]
public class StaffCourtsController(IStaffCourtService service) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet]
    public async Task<IActionResult> GetCourtsAsync(CancellationToken ct, int pageNumber = 1, int pageSize = 20)
        => Ok(await service.GetCourtsAsync(UserId, pageNumber, pageSize, ct));
    [HttpGet("{courtId:guid}")]
    public async Task<IActionResult> GetCourtAsync(Guid courtId, CancellationToken ct) => Ok(await service.GetCourtAsync(UserId, courtId, ct));
    [HttpGet("{courtId:guid}/blocks")]
    public async Task<IActionResult> GetBlocksAsync(Guid courtId, CancellationToken ct, int pageNumber = 1, int pageSize = 20)
        => Ok(await service.GetBlocksAsync(UserId, courtId, pageNumber, pageSize, ct));
    [HttpPost("{courtId:guid}/blocks")]
    public async Task<IActionResult> CreateBlockAsync(Guid courtId, CreateCourtBlockRequest request, CancellationToken ct)
        => Ok(await service.CreateBlockAsync(UserId, courtId, request, ct));
    [HttpDelete("{courtId:guid}/blocks/{blockId:guid}")]
    public async Task<IActionResult> RemoveBlockAsync(Guid courtId, Guid blockId, CancellationToken ct)
    { await service.RemoveBlockAsync(UserId, courtId, blockId, ct); return NoContent(); }
}
