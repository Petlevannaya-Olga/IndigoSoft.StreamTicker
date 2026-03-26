using System.Threading.Channels;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class Pipeline(
    IEnumerable<IWebSocketClient> clients,
    IDeduplicator<Tick> deduplicator,
    ITickRepository repository,
    ILogger<Pipeline> logger)
    : BackgroundService
{
    private readonly Channel<Tick> _channel = Channel.CreateBounded<Tick>(1000);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var clientTasks = clients
            .Select(c => c.RunAsync(_channel.Writer, ct))
            .ToArray();

        var processTask = ProcessTicks(ct);
        var tpsTask = LogTps(ct);

        var allTasks = clientTasks
            .Concat([processTask, tpsTask])
            .ToArray();

        await Task.WhenAll(allTasks);
    }

    private async Task ProcessTicks(CancellationToken ct)
    {
        const int batchSize = 1000;
        var batch = new List<Tick>(batchSize);

        await foreach (var tick in _channel.Reader.ReadAllAsync(ct))
        {
            Interlocked.Increment(ref _tickCount);

            if (deduplicator.IsDuplicate(tick))
                continue;

            // logger.LogInformation("Tick: {Tick}", tick);
            batch.Add(tick);

            if (batch.Count < batchSize) continue;

            var toSave = batch.ToList();
            batch.Clear();

            _ = Task.Run(() => Save(toSave), ct);
        }

        if (batch.Count > 0)
            await Save(batch);
    }

    private int _tickCount;

    private async Task LogTps(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var count = Interlocked.Exchange(ref _tickCount, 0);
            logger.LogInformation("TPS: {Count}", count);

            await Task.Delay(1000, ct);
        }
    }

    private async Task Save(List<Tick> batch)
    {
        try
        {
            await repository.SaveBatchAsync(batch);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving batch");
        }
    }
}