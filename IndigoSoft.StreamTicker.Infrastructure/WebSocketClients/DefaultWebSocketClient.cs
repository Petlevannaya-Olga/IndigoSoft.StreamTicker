using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class DefaultWebSocketClient(
    IWebSocketConnector connector,
    IMessageReceiver receiver,
    IMessageConverter<Tick> converter,
    IWebSocketPolicy policy,
    ILogger<DefaultWebSocketClient> logger)
    : IWebSocketClient<Tick>
{
    public async Task RunAsync(ITargetBlock<Tick> target, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
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

                            if (items is not null)
                            {
                                foreach (var item in items)
                                {
                                    await target.SendAsync(item, pollyCt);
                                }
                            }
                        },
                        pollyCt);

                    throw new Exception("WebSocket disconnected unexpectedly");
                }, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning("WebSocket error, reconnecting in 2 seconds...");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                }
                catch
                {
                    // ignored
                }
            }
        }

        logger.LogInformation("{Client} stopped", GetType().Name);
    }
}