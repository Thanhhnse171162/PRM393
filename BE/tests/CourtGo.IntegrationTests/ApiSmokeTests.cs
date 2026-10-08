using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CourtGo.IntegrationTests;

public class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(WebApplicationFactory<Program> factory) =>
        _client = factory.WithWebHostBuilder(b => b.UseContentRoot(AppContext.BaseDirectory)).CreateClient();

    [Fact]
    public async Task PublicEndpoint_IsReachable_WithoutToken()
    {
        var response = await _client.GetAsync("/api/sports/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_Returns401_WithoutToken()
    {
        var response = await _client.GetAsync("/api/bookings/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoints_Return200()
    {
        var healthRes = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthRes.StatusCode);

        var healthzRes = await _client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, healthzRes.StatusCode);
    }
}
