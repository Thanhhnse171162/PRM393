using CourtGo.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CourtGo.IntegrationTests;

public class PaymentCallbackPortTests
{
    [Theory]
    [InlineData("GET", "/health", true)]
    [InlineData("GET", "/api/payments/vnpay/ipn", true)]
    [InlineData("GET", "/api/payments/vnpay/return", true)]
    [InlineData("GET", "/swagger/v1/swagger.json", false)]
    [InlineData("GET", "/api/bookings", false)]
    [InlineData("POST", "/api/auth/login", false)]
    [InlineData("POST", "/api/payments/vnpay/ipn", false)]
    [InlineData("GET", "/api/payments/vnpay/return/extra", false)]
    public async Task CallbackListenerPublishesOnlyExactGetEndpoints(string method, string path, bool allowed)
    {
        var reached = false;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Payments:CallbackOnlyPort"] = "5081" }).Build();
        var middleware = new PaymentCallbackPortMiddleware(_ => { reached = true; return Task.CompletedTask; }, config);
        var context = new DefaultHttpContext(); context.Connection.LocalPort = 5081;
        context.Request.Method = method; context.Request.Path = path;
        await middleware.InvokeAsync(context);
        Assert.Equal(allowed, reached); Assert.Equal(allowed ? 200 : 404, context.Response.StatusCode);
        reached = false; context.Connection.LocalPort = 5080;
        await middleware.InvokeAsync(context); Assert.True(reached);
    }
}
