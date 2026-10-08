using CourtGo.Application.Reviews;

namespace CourtGo.Application.Interfaces;

public interface IReviewService
{
    Task<ReviewDto> CreateReviewAsync(Guid customerUserId, Guid bookingId, CreateReviewRequest request, CancellationToken ct = default);
    Task<CenterReviewsResponse> GetCenterReviewsAsync(Guid centerId, CenterReviewQuery query, CancellationToken ct = default);
}
