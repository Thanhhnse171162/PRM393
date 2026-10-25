namespace CourtGo.Api.Middleware;

/// <summary>Optional separate listener for a local tunnel. Never publishes the full API surface.</summary>
public sealed class PaymentCallbackPortMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var port = configuration.GetValue<int?>("Payments:CallbackOnlyPort");
        if (port.HasValue && context.Connection.LocalPort == port)
        {
            var allowed = HttpMethods.IsGet(context.Request.Method) && context.Request.Path.Value is
                "/health" or "/api/payments/vnpay/ipn" or "/api/payments/vnpay/return";
            if (!allowed) { context.Response.StatusCode = StatusCodes.Status404NotFound; return; }
        }
        await next(context);
    }
}
