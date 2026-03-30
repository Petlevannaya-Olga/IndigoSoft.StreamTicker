using System.Buffers;
using System.Threading.Channels;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.Pipelines;

public class Pipeline(
    IEnumerable<IWebSocketClient<ChannelWriter<Tick>>> clients,
    ITickRepository repository,
    IDeduplicator deduplicator,
    IMetricsService metrics,
    ILogger<Pipeline> logger)
    : IPipeline
{
    public async Task RunAsync(CancellationToken ct)
    {
        var channel = Channel.CreateBounded<Tick>(new BoundedChannelOptions(50_000)
        {
            SingleWriter = false,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        logger.LogInformation("Pipeline started");

        var consumerTask = ConsumeAsync(channel.Reader, ct);

        var clientTasks = clients
            .Select(c => c.RunAsync(channel.Writer, ct))
            .ToArray();

        await Task.WhenAll(clientTasks);

        channel.Writer.Complete();

        await consumerTask;

        logger.LogInformation("Pipeline completed");
    }

    private async Task ConsumeAsync(ChannelReader<Tick> reader, CancellationToken ct)
    {
        var batchSize = 2000;
        var timeout = TimeSpan.FromSeconds(5);

        var buffer = ArrayPool<Tick>.Shared.Rent(batchSize);
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
                        await FlushAsync(buffer, count);
                        buffer = ArrayPool<Tick>.Shared.Rent(batchSize);
                        count = 0;
                    }
                }

                // периодический flush
                if (await timer.WaitForNextTickAsync(ct))
                {
                    if (count > 0)
                    {
                        await FlushAsync(buffer, count);
                        buffer = ArrayPool<Tick>.Shared.Rent(batchSize);
                        count = 0;
                    }
                }
            }
        }
        finally
        {
            if (count > 0)
            {
                await FlushAsync(buffer, count);
            }

            ArrayPool<Tick>.Shared.Return(buffer);
        }
    }

    private async Task FlushAsync(Tick[] items, int count)
    {
        try
        {
            metrics.IncrementBatch();
            metrics.IncrementTicksCount(count);

            logger.LogInformation("Saving batch of {Count}", count);

            await repository.SaveBatchAsync(items, count, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Save batch failed");
        }
    }
}