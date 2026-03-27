using IndigoSoft.StreamTicker.Application;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class MetricsBackgroundService(
    IMetricsService metrics,
    ILogger<MetricsBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var tps = metrics.GetAndReset();

                logger.LogInformation("TPS: {Tps}", tps);

                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Metrics error");
            }
        }
    }
}