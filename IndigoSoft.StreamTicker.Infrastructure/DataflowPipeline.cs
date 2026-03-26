using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class DataflowPipeline(
    IEnumerable<IWebSocketClient<Tick>> clients,
    ITickRepository repository,
    ILogger<DataflowPipeline> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var source = new BufferBlock<Tick>(new DataflowBlockOptions
        {
            BoundedCapacity = 10_000
        });

        var batch = new BatchBlock<Tick>(1000);

        var writer = new ActionBlock<Tick[]>(async (Tick[] batchItems) =>
            {
                try
                {
                    if (batchItems.Length == 0)
                        return;

                    await repository.SaveBatchAsync(batchItems, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Save batch failed");
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = 10
            });

        // связываем блоки
        source.LinkTo(batch, new DataflowLinkOptions { PropagateCompletion = true });
        batch.LinkTo(writer, new DataflowLinkOptions { PropagateCompletion = true });

        // запускаем клиентов
        var clientTasks = clients
            .Select(c => c.RunAsync(source, ct))
            .ToArray();

        // ждём завершения клиентов
        await Task.WhenAll(clientTasks);

        // корректно завершаем вход
        source.Complete();

        // ждём завершения всей цепочки
        await writer.Completion;

        logger.LogInformation("Pipeline completed");
    }
}