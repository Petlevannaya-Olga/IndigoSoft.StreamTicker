using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class ChannelsWebSocketClient(
    IWebSocketConnector connector,
    IMessageReceiver receiver,
    IMessageConverter converter,
    IWebSocketPolicy policy,
    ILogger<ChannelsWebSocketClient> logger)
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
                            if (!writer.TryWrite(item))
                            {
                                logger.LogError("Dropped tick (channel full)");
                            }
                        }

                        await Task.CompletedTask;
                    },
                    pollyCt);

                throw new Exception("WebSocket disconnected unexpectedly");

            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // normal shutdown
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Unexpected error outside policy");
        }

        logger.LogInformation("{Client} stopped", GetType().Name);
    }
}