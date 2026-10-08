using System.Text.Json.Serialization;

namespace CourtGo.Application.Reviews;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateReviewRequest(byte Rating, string? Comment);

public record ReviewDto(
    Guid Id,
    Guid BookingId,
    byte Rating,
    string? Comment,
    DateTimeOffset CreatedAt);

public record CenterReviewQuery(
    int PageNumber = 1,
    int PageSize = 20,
    byte? Rating = null);

public record CenterReviewItemDto(
    Guid ReviewId,
    byte Rating,
    string? Comment,
    DateTimeOffset CreatedAt,
    string ReviewerDisplayName);

public record CenterReviewsResponse(
    decimal AverageRating,
    int TotalReviews,
    Dictionary<int, int> RatingBreakdown,
    IReadOnlyList<CenterReviewItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalPages);
