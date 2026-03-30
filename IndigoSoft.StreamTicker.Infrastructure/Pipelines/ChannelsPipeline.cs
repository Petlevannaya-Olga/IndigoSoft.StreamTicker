using System.Buffers;
using System.Threading.Channels;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.Pipelines;

public class ChannelsPipeline(
    IEnumerable<IWebSocketClient<ChannelWriter<Tick>>> clients,
    ITickRepository repository,
    IDeduplicator deduplicator,
    IMetricsService metrics,
    ILogger<ChannelsPipeline> logger)
    : IPipeline
{
    public async Task RunAsync(CancellationToken ct)
    {
        var clientList = clients.ToList();

        // 🔹 общий входной канал
        var channel = Channel.CreateBounded<Tick>(new BoundedChannelOptions(50_000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });

        logger.LogInformation("Pipeline started, clients count = {Count}", clientList.Count);

        var clientTasks = clientList
            .Select(c => c.RunAsync(channel.Writer, ct))
            .ToArray();

        // 🔹 processing + batching + writing
        var processingTask = Task.Run(async () =>
        {
            var reader = channel.Reader;

            var batchSize = 2000;
            var timeout = TimeSpan.FromMilliseconds(5000);

            var pool = ArrayPool<Tick>.Shared;
            var buffer = pool.Rent(batchSize);
            var count = 0;

            using var timer = new PeriodicTimer(timeout);

            try
            {
                while (await reader.WaitToReadAsync(ct))
                {
                    while (reader.TryRead(out var tick))
                    {
                        metrics.IncrementIn();

                        if (deduplicator.IsDuplicate(tick))
                        {
                            metrics.IncrementDeduplicated();
                            continue;
                        }

                        metrics.IncrementOut();

                        buffer[count++] = tick;

                        if (count >= batchSize)
                        {
                            await FlushAsync();
                        }
                    }

                    if (await timer.WaitForNextTickAsync(ct))
                    {
                        if (count > 0)
                        {
                            await FlushAsync();
                        }
                    }
                }
            }
            finally
            {
                if (count > 0)
                {
                    await FlushAsync();
                }

                pool.Return(buffer);
            }

            return;

            async Task FlushAsync()
            {
                var items = buffer;
                var size = count;

                buffer = pool.Rent(batchSize);
                count = 0;

                try
                {
                    metrics.IncrementBatch();
                    metrics.IncrementTicksCount(size);

                    logger.LogInformation("{TicksCount} ticks saved to Db", size);

                    await repository.SaveBatchAsync(items, size, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Save batch failed");
                }
                finally
                {
                    ArrayPool<Tick>.Shared.Return(items);
                }
            }

        }, ct);

        await Task.WhenAll(clientTasks);

        channel.Writer.TryComplete();

        await processingTask;

        logger.LogInformation("Pipeline completed");
    }
}