using CourtGo.Application.Interfaces;
using CourtGo.Application.PriceRules;
using CourtGo.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin/courts/{courtId:guid}/price-rules")]
[Authorize(Roles = "Admin")]
public class AdminPriceRulesController(IAdminPriceRuleService priceRuleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminPriceRuleDto>>> GetPriceRulesAsync(
        Guid courtId,
        [FromQuery] CourtGoDayOfWeek? dayOfWeek,
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        return Ok(await priceRuleService.GetPriceRulesAsync(courtId, dayOfWeek, isActive, ct));
    }

    [HttpGet("{priceRuleId:guid}")]
    public async Task<ActionResult<AdminPriceRuleDto>> GetPriceRuleByIdAsync(
        Guid courtId,
        Guid priceRuleId,
        CancellationToken ct)
    {
        return Ok(await priceRuleService.GetPriceRuleByIdAsync(courtId, priceRuleId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<AdminPriceRuleDto>> CreatePriceRuleAsync(
        Guid courtId,
        [FromBody] CreatePriceRuleRequest request,
        CancellationToken ct)
    {
        var result = await priceRuleService.CreatePriceRuleAsync(courtId, request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{priceRuleId:guid}")]
    public async Task<ActionResult<AdminPriceRuleDto>> UpdatePriceRuleAsync(
        Guid courtId,
        Guid priceRuleId,
        [FromBody] UpdatePriceRuleRequest request,
        CancellationToken ct)
    {
        return Ok(await priceRuleService.UpdatePriceRuleAsync(courtId, priceRuleId, request, ct));
    }

    [HttpPatch("{priceRuleId:guid}/status")]
    public async Task<ActionResult<AdminPriceRuleDto>> UpdatePriceRuleStatusAsync(
        Guid courtId,
        Guid priceRuleId,
        [FromBody] UpdatePriceRuleStatusRequest request,
        CancellationToken ct)
    {
        return Ok(await priceRuleService.UpdatePriceRuleStatusAsync(courtId, priceRuleId, request, ct));
    }
}
