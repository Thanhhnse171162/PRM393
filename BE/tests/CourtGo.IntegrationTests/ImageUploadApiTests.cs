using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CourtGo.Application.Interfaces;
using CourtGo.Application.SportCenters;
using CourtGo.Application.Courts;
using CourtGo.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.IntegrationTests;

public class ImageUploadApiTests
{
    private sealed class ImageFactory(string root) : BookingHoldApiTests.Factory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IImageStorage>();
                services.AddSingleton<IImageStorage>(new LocalImageStorage(root));
            });
        }
    }
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aN2kAAAAASUVORK5CYII=");

    [Fact]
    public async Task Upload_ValidatesAccessAndContent_AndPersistsCenterAndCourtImages()
    {
        var root = Path.Combine(Path.GetTempPath(), "courtgo-image-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var factory = new ImageFactory(root);
            using var client = factory.CreateClient();
            async Task<HttpResponseMessage> Upload(byte[] bytes)
            {
                using var body = new MultipartFormDataContent();
                body.Add(new ByteArrayContent(bytes), "file", "../../untrusted-name.png");
                return await client.PostAsync("/api/admin/images", body);
            }
            Assert.Equal(HttpStatusCode.Unauthorized, (await Upload(Png)).StatusCode);
            client.DefaultRequestHeaders.Authorization = new("Bearer", factory.ActiveCustomerToken);
            Assert.Equal(HttpStatusCode.Forbidden, (await Upload(Png)).StatusCode);
            client.DefaultRequestHeaders.Authorization = new("Bearer", factory.StaffToken);
            Assert.Equal(HttpStatusCode.Forbidden, (await Upload(Png)).StatusCode);
            client.DefaultRequestHeaders.Authorization = new("Bearer", factory.AdminToken);
            Assert.Equal(HttpStatusCode.BadRequest, (await Upload("<svg onload='alert(1)'/>"u8.ToArray())).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Upload(new byte[5 * 1024 * 1024 + 1])).StatusCode);
            var saved = await Upload(Png);
            saved.EnsureSuccessStatusCode();
            var url = (await saved.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imageUrl").GetString()!;
            Assert.Matches(@"^/api/images/[a-f0-9]{32}\.png$", url);
            client.DefaultRequestHeaders.Authorization = null;
            var downloaded = await client.GetAsync(url);
            downloaded.EnsureSuccessStatusCode();
            Assert.Equal(Png, await downloaded.Content.ReadAsByteArrayAsync());
            Assert.Equal("image/png", downloaded.Content.Headers.ContentType!.MediaType);
            Assert.Equal("nosniff", downloaded.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/images/not-an-image.txt")).StatusCode);

            client.DefaultRequestHeaders.Authorization = new("Bearer", factory.AdminToken);
            var created = await client.PostAsJsonAsync("/api/admin/sport-centers",
                new CreateSportCenterRequest("Uploaded Image Center", "Street", null, "District", "City", CoverImageUrl: url));
            created.EnsureSuccessStatusCode();
            var center = (await created.Content.ReadFromJsonAsync<AdminSportCenterDetailDto>())!;
            using var db = factory.CreateDbContext();
            var sport = await db.Sports.FirstAsync();
            created = await client.PostAsJsonAsync("/api/admin/courts",
                new CreateCourtRequest(center.CenterId, sport.Id, "UP1", "Uploaded court", CoverImageUrl: url, BasePricePerHour: 100000));
            created.EnsureSuccessStatusCode();
            var court = (await created.Content.ReadFromJsonAsync<AdminCourtDetailDto>())!;
            var second = await Upload(Png);
            var nextUrl = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imageUrl").GetString()!;
            Assert.NotEqual(url, nextUrl);
            (await client.PutAsJsonAsync($"/api/admin/sport-centers/{center.CenterId}",
                new UpdateSportCenterRequest(center.Name, "Street", null, "District", "City", CoverImageUrl: nextUrl))).EnsureSuccessStatusCode();
            (await client.PutAsJsonAsync($"/api/admin/courts/{court.CourtId}",
                new UpdateCourtRequest(court.Name, CoverImageUrl: nextUrl, BasePricePerHour: 100000))).EnsureSuccessStatusCode();
            client.DefaultRequestHeaders.Authorization = null;
            foreach (var path in new[] { $"/api/sport-centers/{center.CenterId}", $"/api/courts/{court.CourtId}" })
                Assert.Equal(nextUrl, (await client.GetFromJsonAsync<JsonElement>(path)).GetProperty("coverImageUrl").GetString());
            Assert.Equal(Png, await client.GetByteArrayAsync(nextUrl));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
