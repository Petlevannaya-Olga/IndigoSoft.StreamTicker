using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class KrakenWebSocketClient(
    IWebSocketConnector connector,
    IMessageReceiver receiver,
    IMessageProcessor<Tick> processor,
    IWebSocketPolicy policy,
    ILogger<KrakenWebSocketClient> logger)
    : WebSocketClientBase<KrakenTickDto, Tick>(connector, receiver, processor, policy, logger)
{
    protected override Uri GetUri()
    {
        return new Uri("wss://ws.kraken.com");
    }
}