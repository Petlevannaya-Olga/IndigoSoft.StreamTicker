using IndigoSoft.StreamTicker.Application;
using Microsoft.Extensions.Hosting;

namespace IndigoSoft.StreamTicker.Infrastructure.BackgroundServices;

public class PipelineBackgroundService(
    IPipeline pipeline)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken ct)
    {
        return pipeline.RunAsync(ct);
    }
}