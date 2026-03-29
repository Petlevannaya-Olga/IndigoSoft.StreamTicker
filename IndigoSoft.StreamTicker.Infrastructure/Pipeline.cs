using System.Buffers;
using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class Pipeline(
    IEnumerable<IWebSocketClient> clients,
    ITickRepository repository,
    IDeduplicator deduplicator,
    IMetricsService metrics,
    ILogger<Pipeline> logger)
    : IPipeline
{
    public async Task RunAsync(CancellationToken ct)
    {
        var source = new BufferBlock<Tick>(new DataflowBlockOptions
        {
            BoundedCapacity = 50_000,
            CancellationToken = ct
        });

        var metricsBlock = new TransformBlock<Tick, Tick>(tick =>
        {
            metrics.IncrementIn();
            return tick;
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
            CancellationToken = ct
        });

        var dedupBlock = new TransformManyBlock<Tick, Tick>(tick =>
        {
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
            CancellationToken = ct
        });

        var batch = CreateBatchBlock<Tick>(
            batchSize: 2000,
            timeout: TimeSpan.FromMilliseconds(1000), // flush
            ct);

        var writer = new ActionBlock<(Tick[] Items, int Count)>(async batchData =>
        {
            try
            {
                if (batchData.Count == 0)
                    return;

                metrics.IncrementBatch();
                logger.LogInformation("{TicksCount} ticks saved to Db", batchData.Count);

                await repository.SaveBatchAsync(
                    batchData.Items,
                    batchData.Count,
                    ct); // не игнорируем ct
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

        T[] buffer = pool.Rent(batchSize);
        int count = 0;

        Timer? timer = null;

        void Flush()
        {
            T[]? batch;
            int size;

            lock (gate)
            {
                if (count == 0)
                    return;

                batch = buffer;
                size = count;

                buffer = pool.Rent(batchSize);
                count = 0;
            }

            output.Post((batch, size));
        }

        timer = new Timer(_ => Flush(), null, timeout, Timeout.InfiniteTimeSpan);

        var input = new ActionBlock<T>(item =>
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

            // перезапускаем таймер при каждом событии
            timer?.Change(timeout, Timeout.InfiniteTimeSpan);

            if (fullBatch != null)
                output.Post((fullBatch, fullSize));
        },
        new ExecutionDataflowBlockOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = 1
        });

        input.Completion.ContinueWith(_ =>
        {
            Flush();
            output.Complete();
            timer?.Dispose();
        }, ct);

        return DataflowBlock.Encapsulate(input, output);
    }
}