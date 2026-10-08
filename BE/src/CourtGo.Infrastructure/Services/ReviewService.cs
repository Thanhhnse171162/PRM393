using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Reviews;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class ReviewService(CourtGoDbContext db, TimeProvider clock) : IReviewService
{
    public async Task<ReviewDto> CreateReviewAsync(Guid customerUserId, Guid bookingId, CreateReviewRequest request, CancellationToken ct = default)
    {
        if (request.Rating is < 1 or > 5)
            throw new ValidationException("Rating must be between 1 and 5.");

        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        if (comment != null && comment.Length > 1000)
            throw new ValidationException("Comment must be at most 1000 characters.");

        var booking = await db.Bookings.AsNoTracking()
            .Where(b => b.Id == bookingId)
            .Select(b => new { b.Id, b.CustomerUserId, b.BookingStatus })
            .SingleOrDefaultAsync(ct);

        if (booking is null || booking.CustomerUserId != customerUserId)
            throw new NotFoundException("Booking not found.", ErrorCodes.BookingNotFound);

        if (booking.BookingStatus != BookingStatus.Completed)
            throw new ConflictException("Only completed bookings can be reviewed.");

        if (await db.Reviews.AnyAsync(r => r.BookingId == bookingId, ct))
            throw new ConflictException("Booking has already been reviewed.");

        var review = new Review
        {
            BookingId = bookingId,
            CustomerUserId = customerUserId,
            Rating = request.Rating,
            Comment = comment,
            IsVisible = true,
            CreatedAt = clock.GetUtcNow()
        };

        db.Reviews.Add(review);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("Booking has already been reviewed.");
        }

        return new ReviewDto(review.Id, review.BookingId, review.Rating, review.Comment, review.CreatedAt);
    }

    public async Task<CenterReviewsResponse> GetCenterReviewsAsync(Guid centerId, CenterReviewQuery query, CancellationToken ct = default)
    {
        if (!await db.SportCenters.AnyAsync(sc => sc.Id == centerId, ct))
            throw new NotFoundException("Sport center not found.");

        StaffOperationsService.ValidatePage(query.PageNumber, query.PageSize);

        var baseQuery = db.Reviews.AsNoTracking()
            .Where(r => r.Booking!.Court!.SportCenterId == centerId && r.IsVisible);

        var totalReviews = await baseQuery.CountAsync(ct);

        var ratingGroups = await baseQuery
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var breakdown = new Dictionary<int, int>
        {
            [1] = 0,
            [2] = 0,
            [3] = 0,
            [4] = 0,
            [5] = 0
        };

        foreach (var g in ratingGroups)
        {
            if (breakdown.ContainsKey(g.Rating))
            {
                breakdown[g.Rating] = g.Count;
            }
        }

        decimal averageRating = totalReviews > 0
            ? Math.Round((decimal)ratingGroups.Sum(g => (long)g.Rating * g.Count) / totalReviews, 1, MidpointRounding.AwayFromZero)
            : 0m;

        var itemsQuery = baseQuery;
        if (query.Rating.HasValue)
        {
            if (query.Rating.Value is < 1 or > 5)
                throw new ValidationException("Rating filter must be between 1 and 5.");
            itemsQuery = itemsQuery.Where(r => r.Rating == query.Rating.Value);
        }

        var filteredCount = await itemsQuery.CountAsync(ct);
        var items = await itemsQuery
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new CenterReviewItemDto(
                r.Id,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                !string.IsNullOrWhiteSpace(r.Booking!.CustomerNameSnapshot)
                    ? r.Booking.CustomerNameSnapshot
                    : (r.CustomerUser != null && !string.IsNullOrWhiteSpace(r.CustomerUser.FullName)
                        ? r.CustomerUser.FullName
                        : "Khách hàng")))
            .ToListAsync(ct);

        int totalPages = (int)Math.Ceiling(filteredCount / (double)query.PageSize);

        return new CenterReviewsResponse(
            averageRating,
            totalReviews,
            breakdown,
            items,
            query.PageNumber,
            query.PageSize,
            totalPages);
    }
}
