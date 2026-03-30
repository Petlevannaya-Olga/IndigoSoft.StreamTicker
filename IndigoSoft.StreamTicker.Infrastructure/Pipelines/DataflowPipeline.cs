using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.Pipelines;

public class DataflowPipeline(
    IEnumerable<IWebSocketClient<ITargetBlock<Tick>>> clients,
    ITickRepository repository,
    IDeduplicator deduplicator,
    IMetricsService metrics,
    ILogger<DataflowPipeline> logger)
    : IPipeline
{
    public async Task RunAsync(CancellationToken ct)
    {
        var clientList = clients.ToList();

        var source = new BufferBlock<Tick>(new DataflowBlockOptions
        {
            BoundedCapacity = 50_000,
            CancellationToken = ct
        });

        var processBlock = new TransformManyBlock<Tick, Tick>(tick =>
            {
                metrics.IncrementIn();

                if (deduplicator.IsDuplicate(tick))
                {
                    metrics.IncrementDeduplicated();
                    return [];
                }

                metrics.IncrementOut();
                return [tick];
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                EnsureOrdered = false,
                CancellationToken = ct
            });

        var batchBlock = new BatchBlock<Tick>(1000, new GroupingDataflowBlockOptions
        {
            BoundedCapacity = 10_000,
            CancellationToken = ct
        });

        var writer = new ActionBlock<Tick[]>(async batch =>
            {
                if (batch.Length == 0)
                    return;

                try
                {
                    metrics.IncrementBatch();
                    metrics.IncrementTicksCount(batch.Length);

                    logger.LogInformation("{TicksCount} ticks saved to Db", batch.Length);

                    await repository.SaveBatchAsync(batch, batch.Length, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Save batch failed");
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 4,
                BoundedCapacity = 10,
                EnsureOrdered = false,
                CancellationToken = ct
            });

        var linkOptions = new DataflowLinkOptions
        {
            PropagateCompletion = true
        };

        source.LinkTo(processBlock, linkOptions);
        processBlock.LinkTo(batchBlock, linkOptions);
        batchBlock.LinkTo(writer, linkOptions);

        logger.LogInformation("Pipeline started, clients count = {Count}", clientList.Count);

        var clientTasks = clientList
            .Select(c => c.RunAsync(source, ct))
            .ToArray();

        await Task.WhenAll(clientTasks);

        source.Complete();

        await writer.Completion;

        logger.LogInformation("Pipeline completed");
    }
}