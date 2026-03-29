using System.Buffers;
using System.Threading.Channels;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.Pipelines;

public class ChannelPipeline(
    IEnumerable<IWebSocketClient<ChannelWriter<Tick>>> clients,
    ITickRepository repository,
    IDeduplicator deduplicator,
    IMetricsService metrics,
    ILogger<ChannelPipeline> logger)
    : IPipeline
{
    public async Task RunAsync(CancellationToken ct)
    {
        var tickChannel = Channel.CreateBounded<Tick>(new BoundedChannelOptions(50_000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        var batchChannel = Channel.CreateBounded<(Tick[] Items, int Count)>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        var dispatcherTask = RunDispatcherAsync(tickChannel.Reader, batchChannel.Writer, ct);

        var workerTasks = Enumerable.Range(0, Environment.ProcessorCount)
            .Select(_ => RunWorkerAsync(batchChannel.Reader, ct))
            .ToArray();

        var producerTasks = clients
            .Select(c => c.RunAsync(tickChannel.Writer, ct))
            .ToArray();

        await Task.WhenAll(producerTasks);

        tickChannel.Writer.Complete();
        await dispatcherTask;

        batchChannel.Writer.Complete();
        await Task.WhenAll(workerTasks);

        logger.LogInformation("Pipeline completed");
    }

    private async Task RunDispatcherAsync(
        ChannelReader<Tick> reader,
        ChannelWriter<(Tick[] Items, int Count)> writer,
        CancellationToken ct)
    {
        var pool = ArrayPool<Tick>.Shared;

        const int batchSize = 2000;
        var batch = pool.Rent(batchSize);
        var count = 0;

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000));

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var readTask = reader.WaitToReadAsync(ct).AsTask();
                var timerTask = timer.WaitForNextTickAsync(ct).AsTask();

                var completed = await Task.WhenAny(readTask, timerTask);

                if (completed == timerTask)
                {
                    await FlushAsync(batch, count);
                    continue;
                }

                if (!await readTask)
                    break;

                while (reader.TryRead(out var tick))
                {
                    metrics.IncrementIn();

                    if (deduplicator.IsDuplicate(tick))
                    {
                        metrics.IncrementDeduplicated();
                        continue;
                    }

                    metrics.IncrementOut();

                    batch[count++] = tick;

                    if (count >= batchSize)
                    {
                        await FlushAsync(batch, count);

                        batch = pool.Rent(batchSize);
                        count = 0;
                    }
                }
            }

            await FlushAsync(batch, count);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dispatcher failed");
        }
        finally
        {
            pool.Return(batch);
        }

        async Task FlushAsync(Tick[] buffer, int size)
        {
            if (size == 0)
                return;

            var toSend = pool.Rent(size);
            Array.Copy(buffer, toSend, size);

            try
            {
                await writer.WriteAsync((toSend, size), ct);
            }
            catch
            {
                pool.Return(toSend);
                throw;
            }

            pool.Return(buffer);
        }
    }

    private async Task RunWorkerAsync(
        ChannelReader<(Tick[] Items, int Count)> reader,
        CancellationToken ct)
    {
        var pool = ArrayPool<Tick>.Shared;

        await foreach (var batch in reader.ReadAllAsync(ct))
        {
            try
            {
                if (batch.Count == 0)
                    continue;

                metrics.IncrementBatch();

                logger.LogInformation("{TicksCount} ticks saved to Db", batch.Count);

                await repository.SaveBatchAsync(batch.Items, batch.Count, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Save batch failed");
            }
            finally
            {
                pool.Return(batch.Items);
            }
        }
    }
}