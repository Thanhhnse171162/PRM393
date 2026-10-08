using CourtGo.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace CourtGo.Api.Workers;
public sealed class BookingLifecycleWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<BookingLifecycleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("Workers:Enabled", true)) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(config.GetValue("Workers:LifecycleIntervalSeconds", 60), 5, 3600)));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var count = await scope.ServiceProvider.GetRequiredService<IBookingLifecycleService>().AdvanceAsync(stoppingToken);
                    if (count > 0) logger.LogInformation("Advanced {Count} booking lifecycle states.", count);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { logger.LogError(ex, "Booking lifecycle pass failed."); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
