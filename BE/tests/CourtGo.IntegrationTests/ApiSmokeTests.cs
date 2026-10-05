using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CourtGo.IntegrationTests;

public class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

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
}
