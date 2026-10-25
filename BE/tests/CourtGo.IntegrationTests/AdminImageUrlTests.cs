using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CourtGo.Application.Courts;
using CourtGo.Application.SportCenters;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CourtGo.IntegrationTests;

public class AdminImageUrlTests
{
    [Fact]
    public async Task ImageUrls_Create_Update_Clear_AreConsistentForAdminAndCustomer()
    {
        using var factory = new BookingHoldApiTests.Factory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.AdminToken);
        using var db = factory.CreateDbContext();
        var sport = await db.Sports.FirstAsync(s => s.IsActive);

        var response = await client.PostAsJsonAsync("/api/admin/sport-centers",
            new CreateSportCenterRequest("Image QA Center", "123 Street", null, "District", "City",
                CoverImageUrl: "https://images.example/center-original.jpg"));
        response.EnsureSuccessStatusCode();
        var center = (await response.Content.ReadFromJsonAsync<AdminSportCenterDetailDto>())!;
        response = await client.PostAsJsonAsync("/api/admin/courts",
            new CreateCourtRequest(center.CenterId, sport.Id, "IMG1", "Image QA Court",
                CoverImageUrl: "https://images.example/court-original.jpg", BasePricePerHour: 150000));
        response.EnsureSuccessStatusCode();
        var court = (await response.Content.ReadFromJsonAsync<AdminCourtDetailDto>())!;
        Assert.Equal("https://images.example/center-original.jpg", center.CoverImageUrl);
        Assert.Equal("https://images.example/court-original.jpg", court.CoverImageUrl);

        async Task AssertImages(string? centerImage, string? courtImage)
        {
            async Task<JsonElement> Get(string path) => await client.GetFromJsonAsync<JsonElement>(path);
            var centerList = await Get("/api/admin/sport-centers?search=Image%20QA%20Center");
            Assert.Equal(centerImage, centerList.GetProperty("items")[0].GetProperty("coverImageUrl").GetString());
            var courtList = await Get($"/api/admin/courts?centerId={center.CenterId}");
            Assert.Equal(courtImage, courtList.GetProperty("items")[0].GetProperty("coverImageUrl").GetString());
            var publicCenters = await Get("/api/sport-centers?search=Image%20QA%20Center");
            Assert.Equal(centerImage, publicCenters[0].GetProperty("coverImageUrl").GetString());
            var publicCenter = await Get($"/api/sport-centers/{center.CenterId}");
            Assert.Equal(courtImage, publicCenter.GetProperty("courts")[0].GetProperty("coverImageUrl").GetString());
            foreach (var path in new[] { $"/api/admin/sport-centers/{center.CenterId}", $"/api/sport-centers/{center.CenterId}" })
                Assert.Equal(centerImage, (await Get(path)).GetProperty("coverImageUrl").GetString());
            foreach (var path in new[] { $"/api/admin/courts/{court.CourtId}", $"/api/courts/{court.CourtId}" })
                Assert.Equal(courtImage, (await Get(path)).GetProperty("coverImageUrl").GetString());
        }

        await AssertImages(center.CoverImageUrl, court.CoverImageUrl);
        foreach (var suffix in new string?[] { "updated.jpg", null })
        {
            var centerImage = suffix == null ? null : $"https://images.example/center-{suffix}";
            var courtImage = suffix == null ? null : $"https://images.example/court-{suffix}";
            response = await client.PutAsJsonAsync($"/api/admin/sport-centers/{center.CenterId}",
                new UpdateSportCenterRequest(center.Name, "123 Street", null, "District", "City", CoverImageUrl: centerImage));
            response.EnsureSuccessStatusCode();
            response = await client.PutAsJsonAsync($"/api/admin/courts/{court.CourtId}",
                new UpdateCourtRequest(court.Name, CoverImageUrl: courtImage, BasePricePerHour: 150000));
            response.EnsureSuccessStatusCode();
            await AssertImages(centerImage, courtImage);
        }
    }
}
