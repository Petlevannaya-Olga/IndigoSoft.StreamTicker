using System.Threading.Channels;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class ChannelWebSocketClient(
    IWebSocketConnector connector,
    IMessageReceiver receiver,
    IMessageConverter converter,
    IWebSocketPolicy policy,
    ILogger<ChannelWebSocketClient> logger)
    : IWebSocketClient<ChannelWriter<Tick>>
{
    public async Task RunAsync(ChannelWriter<Tick> writer, CancellationToken ct)
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
                            // backpressure: если канал перегружен — ждём
                            await writer.WriteAsync(item, pollyCt);
                        }

                        await Task.CompletedTask;
                    },
                    pollyCt);

                // Если вышли из ReceiveAsync — считаем это ошибкой для reconnect
                throw new Exception("WebSocket disconnected unexpectedly");

            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // нормальное завершение
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Unexpected error outside policy");
        }

        logger.LogInformation("{Client} stopped", GetType().Name);
    }
}