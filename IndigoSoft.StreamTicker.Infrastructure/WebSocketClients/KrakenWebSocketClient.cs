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
    ILogger<WebSocketClientBase<KrakenTickDto, Tick>> logger)
    : WebSocketClientBase<KrakenTickDto, Tick>(connector, receiver, processor, policy, logger)
{
    protected override Uri GetUri()
    {
        throw new NotImplementedException();
    }
}