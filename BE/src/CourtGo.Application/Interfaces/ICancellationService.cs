using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Operations;

namespace CourtGo.Application.Interfaces;

public interface ICancellationService
{
    Task<CancellationRequestDto> CreateRequestAsync(Guid customerId, Guid bookingId, CreateCancellationRequest request, CancellationToken ct);
    Task<PagedResult<CancellationRequestDto>> GetRequestsAsync(Guid customerId, Guid bookingId, int pageNumber, int pageSize, CancellationToken ct);
    Task<PagedResult<CancellationRequestDto>> GetAdminRequestsAsync(string? status, int pageNumber, int pageSize, CancellationToken ct);
    Task<CancellationDetailDto> GetAdminRequestAsync(Guid requestId, CancellationToken ct);
    Task<CancellationRequestDto> DecideAsync(Guid adminId, Guid requestId, CancellationDecisionRequest request, CancellationToken ct);
    Task<CancellationRequestDto> ApplyVerifiedRefundAsync(VerifiedPaymentResult result, CancellationToken ct);

    Task<PagedResult<AdminCancellationRequestSummaryDto>> GetAdminRequestsAsync(AdminCancellationRequestQuery query, CancellationToken ct = default);
    Task<AdminCancellationRequestDetailDto> GetAdminRequestDetailAsync(Guid requestId, CancellationToken ct = default);
    Task<CancellationDecisionResponse> ProcessDecisionAsync(Guid adminUserId, Guid requestId, CancellationDecisionRequest request, CancellationToken ct = default);
}
