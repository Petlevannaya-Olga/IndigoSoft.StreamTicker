using System.Buffers;
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
                return Array.Empty<Tick>();
            }

            metrics.IncrementOut();
            return [tick];
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
            EnsureOrdered = false,
            CancellationToken = ct
        });

        var batch = CreateBatchBlock<Tick>(
            batchSize: 2000,
            timeout: TimeSpan.FromMilliseconds(5000),
            ct);

        var writer = new ActionBlock<(Tick[] Items, int Count)>(async batchData =>
        {
            try
            {
                if (batchData.Count == 0)
                    return;

                metrics.IncrementBatch();
                metrics.IncrementTicksCount(batchData.Count);
                logger.LogInformation("{TicksCount} ticks saved to Db", batchData.Count);

                await repository.SaveBatchAsync(
                    batchData.Items,
                    batchData.Count,
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Save batch failed");
            }
            finally
            {
                ArrayPool<Tick>.Shared.Return(batchData.Items);
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
        processBlock.LinkTo(batch, linkOptions);
        batch.LinkTo(writer, linkOptions);

        logger.LogInformation("Pipeline started, clients count = {Count}", clientList.Count);

        var clientTasks = clientList
            .Select(c => c.RunAsync(source, ct))
            .ToArray();

        await Task.WhenAll(clientTasks);

        source.Complete();

        await writer.Completion;

        logger.LogInformation("Pipeline completed");
    }

    private static IPropagatorBlock<T, (T[] Items, int Count)> CreateBatchBlock<T>(
        int batchSize,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var pool = ArrayPool<T>.Shared;

        var output = new BufferBlock<(T[] Items, int Count)>(new DataflowBlockOptions
        {
            BoundedCapacity = 20,
            CancellationToken = ct
        });

        var gate = new object();

        var buffer = pool.Rent(batchSize);
        var count = 0;

        var input = new ActionBlock<T>(async item =>
        {
            T[]? fullBatch = null;
            var fullSize = 0;

            lock (gate)
            {
                buffer[count++] = item;

                if (count >= batchSize)
                {
                    fullBatch = buffer;
                    fullSize = count;

                    buffer = pool.Rent(batchSize);
                    count = 0;
                }
            }

            if (fullBatch != null)
            {
                try
                {
                    await output.SendAsync((fullBatch, fullSize), ct);
                }
                catch (OperationCanceledException)
                {
                    pool.Return(fullBatch);
                }
            }
        },
        new ExecutionDataflowBlockOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = 1,
            EnsureOrdered = false
        });

        _ = Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(timeout, ct);
                    await FlushAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }, ct);

        // гарантирует завершение flush
        input.Completion.ContinueWith(_ =>
        {
            FlushAsync().GetAwaiter().GetResult();
            output.Complete();
        }, CancellationToken.None);

        return DataflowBlock.Encapsulate(input, output);

        async Task FlushAsync()
        {
            T[]? batch = null;
            var size = 0;

            lock (gate)
            {
                if (count == 0)
                    return;

                batch = buffer;
                size = count;

                buffer = pool.Rent(batchSize);
                count = 0;
            }

            try
            {
                await output.SendAsync((batch, size), ct);
            }
            catch (OperationCanceledException)
            {
                pool.Return(batch);
            }
        }
    }
}