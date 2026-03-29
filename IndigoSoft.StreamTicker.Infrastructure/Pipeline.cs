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
            BoundedCapacity = 50_000
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
                    return Enumerable.Empty<Tick>();;
                }

                metrics.IncrementOut(); // прошёл дальше
                return [tick];
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount * 2
            });

        //var batch = new BatchBlock<Tick>(2000);
        var batch = CreateBatchBlock<Tick>(
            batchSize: 2000,
            timeout: TimeSpan.FromSeconds(5),
            ct);
        
        var writer = new ActionBlock<Tick[]>(async (Tick[] batchItems) =>
            {
                try
                {
                    if (batchItems.Length == 0)
                        return;
                    
                    metrics.IncrementBatch(); // новый батч
                    
                    await repository.SaveBatchAsync(batchItems, ct); // игнорировать отмену во время записи?
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
                EnsureOrdered = false
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
    
    private static IPropagatorBlock<T, T[]> CreateBatchBlock<T>(
        int batchSize,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var buffer = new List<T>(batchSize);
        var gate = new object();

        var output = new BufferBlock<T[]>(new DataflowBlockOptions
        {
            BoundedCapacity = 20 // опционально, но полезно
        });

        var timer = new Timer(_ =>
        {
            Flush();
        }, null, timeout, timeout);

        var input = new ActionBlock<T>(item =>
            {
                T[]? batch = null;

                lock (gate)
                {
                    buffer.Add(item);

                    if (buffer.Count >= batchSize)
                    {
                        batch = buffer.ToArray();
                        buffer.Clear();
                    }
                }

                if (batch != null)
                    output.Post(batch);
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
            timer.Dispose();
        }, ct);

        return DataflowBlock.Encapsulate(input, output);

        void Flush()
        {
            T[]? batch = null;

            lock (gate)
            {
                if (buffer.Count == 0)
                    return;

                batch = buffer.ToArray();
                buffer.Clear();
            }

            output.Post(batch);
        }
    }
    
    private static IPropagatorBlock<T, T[]> CreatePooledBatchBlock<T>(
        int batchSize,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var pool = ArrayPool<T>.Shared;

        var buffer = pool.Rent(batchSize);
        var count = 0;

        var gate = new object();

        var output = new BufferBlock<T[]>(new DataflowBlockOptions
        {
            BoundedCapacity = 20
        });

        var timer = new Timer(_ => Flush(), null, timeout, timeout);

        var input = new ActionBlock<T>(item =>
            {
                T[]? batch = null;

                lock (gate)
                {
                    buffer[count++] = item;

                    if (count >= batchSize)
                    {
                        batch = buffer;
                        buffer = pool.Rent(batchSize);
                        count = 0;
                    }
                }

                if (batch != null)
                    output.Post(batch);
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
            timer.Dispose();
        }, ct);

        return DataflowBlock.Encapsulate(input, output);

        void Flush()
        {
            T[]? batch = null;

            lock (gate)
            {
                if (count == 0)
                    return;

                batch = buffer;
                buffer = pool.Rent(batchSize);
                count = 0;
            }

            output.Post(batch);
        }
    }
}