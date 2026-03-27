using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class ByBitWebSocketClient(
    IWebSocketConnector connector,
    IMessageReceiver receiver,
    IMessageProcessor<Tick> processor,
    IWebSocketPolicy policy,
    ILogger<ByBitWebSocketClient> logger) :
    WebSocketClientBase<ByBitTickDto, Tick>(connector, receiver, processor, policy, logger)
{
    protected override Uri GetUri()
    {
        return new Uri("wss://stream.bybit.com/v5/public/spot");
    }
}