using CourtGo.Application.Cancellation;

namespace CourtGo.Application.Interfaces;

public interface IAdminCancellationPolicyService
{
    Task<IReadOnlyList<AdminCancellationPolicySummaryDto>> GetPoliciesAsync(CancellationToken ct = default);
    Task<AdminCancellationPolicyDetailDto> GetPolicyByIdAsync(Guid policyId, CancellationToken ct = default);
    Task<AdminCancellationPolicyDetailDto> CreatePolicyAsync(CreateCancellationPolicyRequest request, CancellationToken ct = default);
    Task<AdminCancellationPolicyDetailDto> ActivatePolicyAsync(Guid policyId, CancellationToken ct = default);
}
