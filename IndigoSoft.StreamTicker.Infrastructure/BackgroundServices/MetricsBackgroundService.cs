using IndigoSoft.StreamTicker.Application;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.BackgroundServices;

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

                logger.LogInformation(
                    "IN: {TpsIn}, OUT: {TpsOut}, BatchCount: {BatchCount}, Deduplicated: {DeduplicatedCount}",
                    tps.In,
                    tps.Out,
                    tps.BatchCount,
                    tps.Deduplicated);

                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError("Metrics error");
            }
        }
    }
}