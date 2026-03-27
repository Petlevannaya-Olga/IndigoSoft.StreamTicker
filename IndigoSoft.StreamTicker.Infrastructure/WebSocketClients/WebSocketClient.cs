using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Application;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class WebSocketClient<TDto, TDomain>(
    IWebSocketConnector connector,
    IMessageReceiver receiver,
    IMessageProcessor<TDomain> processor,
    IWebSocketPolicy policy,
    ILogger<WebSocketClient<TDto, TDomain>> logger)
    : IWebSocketClient<TDomain> where TDomain : class
{
    public async Task RunAsync(ITargetBlock<TDomain> target, CancellationToken ct)
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
                            var items = processor.ProcessMessage(message, pollyCt);

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