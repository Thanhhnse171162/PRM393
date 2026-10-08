using CourtGo.Application.PriceRules;
using CourtGo.Domain.Enums;

namespace CourtGo.Application.Interfaces;

public interface IAdminPriceRuleService
{
    Task<IReadOnlyList<AdminPriceRuleDto>> GetPriceRulesAsync(Guid courtId, CourtGoDayOfWeek? dayOfWeek = null, bool? isActive = null, CancellationToken ct = default);
    Task<AdminPriceRuleDto> GetPriceRuleByIdAsync(Guid courtId, Guid priceRuleId, CancellationToken ct = default);
    Task<AdminPriceRuleDto> CreatePriceRuleAsync(Guid courtId, CreatePriceRuleRequest request, CancellationToken ct = default);
    Task<AdminPriceRuleDto> UpdatePriceRuleAsync(Guid courtId, Guid priceRuleId, UpdatePriceRuleRequest request, CancellationToken ct = default);
    Task<AdminPriceRuleDto> UpdatePriceRuleStatusAsync(Guid courtId, Guid priceRuleId, UpdatePriceRuleStatusRequest request, CancellationToken ct = default);
}
