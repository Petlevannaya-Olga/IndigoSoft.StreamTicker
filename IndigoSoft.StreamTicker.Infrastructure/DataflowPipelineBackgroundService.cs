using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class DataflowPipelineBackgroundService(
    IEnumerable<IWebSocketClient> clients,
    ITickRepository repository,
    IDeduplicator deduplicator,
    IMetricsService metrics,
    ILogger<DataflowPipelineBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var source = new BufferBlock<Tick>(new DataflowBlockOptions
        {
            BoundedCapacity = 10_000
        });

        var metricsBlock = new TransformBlock<Tick, Tick>(tick =>
            {
                metrics.IncrementIn(); // входящий тик
                // logger.LogInformation("Exchange = {Exchange}, Symbol = {Symbol}, Price = {Price}", tick.Exchange, tick.Symbol, tick.Price);
                return tick;
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount * 2
            });

        var dedupBlock = new TransformManyBlock<Tick, Tick>(tick =>
            {
                if (deduplicator.IsDuplicate(tick))
                {
                    metrics.IncrementDeduplicated(); // отфильтровано
                    return [];
                }

                metrics.IncrementOut(); // прошёл дальше
                return [tick];
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount * 2
            });

        var batch = new BatchBlock<Tick>(2000); 

        var writer = new ActionBlock<Tick[]>(async (Tick[] batchItems) =>
            {
                try
                {
                    if (batchItems.Length == 0)
                        return;
                    
                    metrics.IncrementBatch(); // новый батч
                    
                    await repository.SaveBatchAsync(batchItems, CancellationToken.None); // игнорировать отмену во время записи
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Save batch failed");
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 4,
                BoundedCapacity = 20
            });

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        source.LinkTo(metricsBlock, linkOptions);
        metricsBlock.LinkTo(dedupBlock, linkOptions);
        dedupBlock.LinkTo(batch, linkOptions);
        batch.LinkTo(writer, linkOptions);

        var clientTasks = clients
            .Select(c => c.RunAsync(source, ct))
            .ToArray();

        await Task.WhenAll(clientTasks);

        source.Complete();

        await writer.Completion;

        logger.LogInformation("Pipeline completed");
    }
}