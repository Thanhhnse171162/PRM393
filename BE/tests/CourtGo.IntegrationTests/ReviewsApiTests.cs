using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CourtGo.Application.Reviews;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CourtGo.IntegrationTests;

public class ReviewsApiTests : IDisposable
{
    private readonly BookingHoldApiTests.Factory _factory = new();
    private readonly HttpClient _client;

    public ReviewsApiTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<Guid> SeedBookingAsync(Guid customerUserId, BookingStatus status, Guid? courtId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();

        var court = courtId.HasValue
            ? await db.Courts.SingleAsync(c => c.Id == courtId.Value)
            : await db.Courts.SingleAsync(c => c.Id == _factory.ActiveCourtId);

        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var booking = new Booking
        {
            BookingCode = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant(),
            CourtId = court.Id,
            CustomerUserId = customerUserId,
            CustomerNameSnapshot = "Customer Name",
            CustomerPhoneSnapshot = "0901111111",
            CourtNameSnapshot = court.Name,
            CenterNameSnapshot = "CourtGo Center Q7",
            SportNameSnapshot = "Cầu lông",
            StartAt = start,
            EndAt = start.AddHours(1),
            DurationMinutes = 60,
            TotalAmount = 100000m,
            DepositAmount = 30000m,
            BookingStatus = status,
            PaymentStatus = BookingPaymentStatus.FullyPaid
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking.Id;
    }

    [Fact]
    public async Task CreateReview_CompletedBooking_Succeeds()
    {
        var bookingId = await SeedBookingAsync(_factory.ActiveCustomer.Id, BookingStatus.Completed);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var response = await _client.PostAsJsonAsync($"/api/bookings/{bookingId}/reviews",
            new CreateReviewRequest(5, "Sân rất đẹp và sạch sẽ."));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var review = await response.Content.ReadFromJsonAsync<ReviewDto>();
        Assert.NotNull(review);
        Assert.Equal(bookingId, review.BookingId);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Sân rất đẹp và sạch sẽ.", review.Comment);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var dbReview = await db.Reviews.SingleOrDefaultAsync(r => r.BookingId == bookingId);
        Assert.NotNull(dbReview);
        Assert.Equal(_factory.ActiveCustomer.Id, dbReview.CustomerUserId);
    }

    [Fact]
    public async Task CreateReview_NonOwner_RejectedWithNotFound()
    {
        var bookingId = await SeedBookingAsync(_factory.ActiveCustomer.Id, BookingStatus.Completed);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.SecondCustomerToken);
        var response = await _client.PostAsJsonAsync($"/api/bookings/{bookingId}/reviews",
            new CreateReviewRequest(5, "Tốt"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(BookingStatus.PendingPayment)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.InProgress)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Expired)]
    [InlineData(BookingStatus.NoShow)]
    public async Task CreateReview_NonCompletedBooking_RejectedWithConflict(BookingStatus status)
    {
        var bookingId = await SeedBookingAsync(_factory.ActiveCustomer.Id, status);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var response = await _client.PostAsJsonAsync($"/api/bookings/{bookingId}/reviews",
            new CreateReviewRequest(4, "Chưa chơi xong đã đánh giá"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateReview_InvalidRating_RejectedWithBadRequest(byte rating)
    {
        var bookingId = await SeedBookingAsync(_factory.ActiveCustomer.Id, BookingStatus.Completed);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var response = await _client.PostAsJsonAsync($"/api/bookings/{bookingId}/reviews",
            new CreateReviewRequest(rating, "Đánh giá"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateReview_SecondReview_RejectedWithConflict()
    {
        var bookingId = await SeedBookingAsync(_factory.ActiveCustomer.Id, BookingStatus.Completed);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var first = await _client.PostAsJsonAsync($"/api/bookings/{bookingId}/reviews",
            new CreateReviewRequest(5, "Lần đầu"));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsJsonAsync($"/api/bookings/{bookingId}/reviews",
            new CreateReviewRequest(4, "Lần hai"));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CenterReviews_AggregatesBreakdownAndExcludesOtherCenters()
    {
        // Center 1 bookings
        var b1 = await SeedBookingAsync(_factory.ActiveCustomer.Id, BookingStatus.Completed);
        var b2 = await SeedBookingAsync(_factory.SecondCustomer.Id, BookingStatus.Completed);

        // Center 2 (other center) setup
        Guid otherCenterId;
        Guid otherBookingId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var otherCenter = new SportCenter
            {
                Name = "Other Center",
                AddressLine = "123 Other St",
                City = "HCM",
                District = "District 1",
                Status = SportCenterStatus.Active
            };
            db.SportCenters.Add(otherCenter);
            var sport = await db.Sports.FirstAsync();
            var otherCourt = new Court
            {
                SportCenter = otherCenter,
                SportId = sport.Id,
                Code = "OTHER1",
                Name = "Sân khác",
                BasePricePerHour = 100000m,
                Status = CourtStatus.Active
            };
            db.Courts.Add(otherCourt);
            await db.SaveChangesAsync();

            otherCenterId = otherCenter.Id;
            otherBookingId = await SeedBookingAsync(_factory.ActiveCustomer.Id, BookingStatus.Completed, otherCourt.Id);
        }

        // Add reviews: Center 1 gets rating 5 and rating 3 (avg = 4.0)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        await _client.PostAsJsonAsync($"/api/bookings/{b1}/reviews", new CreateReviewRequest(5, "Tuyệt vời"));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.SecondCustomerToken);
        await _client.PostAsJsonAsync($"/api/bookings/{b2}/reviews", new CreateReviewRequest(3, "Bình thường"));

        // Other center gets rating 1
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        await _client.PostAsJsonAsync($"/api/bookings/{otherBookingId}/reviews", new CreateReviewRequest(1, "Sân tệ"));

        // Query Center 1 reviews (public/anonymous)
        _client.DefaultRequestHeaders.Authorization = null;
        var center1Res = await _client.GetFromJsonAsync<CenterReviewsResponse>($"/api/sport-centers/{_factory.CenterId}/reviews");

        Assert.NotNull(center1Res);
        Assert.Equal(2, center1Res.TotalReviews);
        Assert.Equal(4.0m, center1Res.AverageRating);
        Assert.Equal(1, center1Res.RatingBreakdown[5]);
        Assert.Equal(1, center1Res.RatingBreakdown[3]);
        Assert.Equal(0, center1Res.RatingBreakdown[1]);
        Assert.Equal(2, center1Res.Items.Count);

        // Ensure safe display name without phone/email
        Assert.All(center1Res.Items, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.ReviewerDisplayName));
            Assert.DoesNotContain("@", item.ReviewerDisplayName);
            Assert.DoesNotContain("090", item.ReviewerDisplayName);
        });

        // Filter Center 1 reviews by rating = 5
        var filteredRes = await _client.GetFromJsonAsync<CenterReviewsResponse>($"/api/sport-centers/{_factory.CenterId}/reviews?rating=5");
        Assert.NotNull(filteredRes);
        Assert.Single(filteredRes.Items);
        Assert.Equal(5, filteredRes.Items[0].Rating);

        // Center 2 reviews isolation
        var center2Res = await _client.GetFromJsonAsync<CenterReviewsResponse>($"/api/sport-centers/{otherCenterId}/reviews");
        Assert.NotNull(center2Res);
        Assert.Equal(1, center2Res.TotalReviews);
        Assert.Equal(1.0m, center2Res.AverageRating);
        Assert.Single(center2Res.Items);
        Assert.Equal(1, center2Res.Items[0].Rating);
    }
}
