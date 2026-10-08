using CourtGo.Application.Interfaces;
using CourtGo.Application.OperatingHours;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin/sport-centers/{centerId:guid}")]
[Authorize(Roles = "Admin")]
public class AdminOperatingHoursController(IAdminOperatingHourService operatingHourService) : ControllerBase
{
    [HttpGet("operating-hours")]
    public async Task<ActionResult<AdminCenterOperatingHoursDto>> GetOperatingHoursAsync(
        Guid centerId,
        CancellationToken ct)
    {
        return Ok(await operatingHourService.GetOperatingHoursAsync(centerId, ct));
    }

    [HttpPut("operating-hours")]
    public async Task<ActionResult<AdminCenterOperatingHoursDto>> UpdateOperatingHoursAsync(
        Guid centerId,
        [FromBody] UpdateOperatingHoursRequest request,
        CancellationToken ct)
    {
        return Ok(await operatingHourService.UpdateOperatingHoursAsync(centerId, request, ct));
    }

    [HttpGet("operating-hour-exceptions")]
    public async Task<ActionResult<IReadOnlyList<AdminOperatingHourExceptionDto>>> GetOperatingHourExceptionsAsync(
        Guid centerId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        return Ok(await operatingHourService.GetOperatingHourExceptionsAsync(centerId, from, to, ct));
    }

    [HttpPost("operating-hour-exceptions")]
    public async Task<ActionResult<AdminOperatingHourExceptionDto>> CreateOperatingHourExceptionAsync(
        Guid centerId,
        [FromBody] CreateOperatingHourExceptionRequest request,
        CancellationToken ct)
    {
        var result = await operatingHourService.CreateOperatingHourExceptionAsync(centerId, request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("operating-hour-exceptions/{exceptionId:guid}")]
    public async Task<ActionResult<AdminOperatingHourExceptionDto>> UpdateOperatingHourExceptionAsync(
        Guid centerId,
        Guid exceptionId,
        [FromBody] UpdateOperatingHourExceptionRequest request,
        CancellationToken ct)
    {
        return Ok(await operatingHourService.UpdateOperatingHourExceptionAsync(centerId, exceptionId, request, ct));
    }

    [HttpDelete("operating-hour-exceptions/{exceptionId:guid}")]
    public async Task<IActionResult> DeleteOperatingHourExceptionAsync(
        Guid centerId,
        Guid exceptionId,
        CancellationToken ct)
    {
        await operatingHourService.DeleteOperatingHourExceptionAsync(centerId, exceptionId, ct);
        return NoContent();
    }
}
