using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class DataflowWebSocketClient(
    IWebSocketConnector connector,
    IMessageReceiver receiver,
    IMessageConverter converter,
    IWebSocketPolicy policy,
    ILogger<DataflowWebSocketClient> logger)
    : IWebSocketClient<ITargetBlock<Tick>>
{
    public async Task RunAsync(ITargetBlock<Tick> target, CancellationToken ct)
    {
        try
        {
            await policy.ExecuteAsync(async pollyCt =>
            {
                var ws = await connector.ConnectAsync(pollyCt);

                await receiver.ReceiveAsync(
                    ws,
                    async message =>
                    {
                        var items = converter.Convert(message, pollyCt);

                        if (items is null)
                            return;

                        foreach (var item in items)
                        {
                            var accepted = await target.SendAsync(item, pollyCt);
                            if(!accepted)
                                logger.LogError("Dropped ticket");
                        }

                        await Task.CompletedTask;
                    },
                    pollyCt);

                // Если вышли из ReceiveAsync — считаем это ошибкой для того, чтобы Polly инициировал reconnect
                throw new Exception("WebSocket disconnected unexpectedly");
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // нормальное завершение при остановке сервиса
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Unexpected error outside policy");
        }

        logger.LogInformation("{Client} stopped", GetType().Name);
    }
}