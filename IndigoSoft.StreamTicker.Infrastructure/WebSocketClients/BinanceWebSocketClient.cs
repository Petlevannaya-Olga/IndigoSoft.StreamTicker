using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class BinanceWebSocketClient(IWebSocketConnector connector, IMessageReceiver receiver, IMessageProcessor<Tick> processor, IWebSocketPolicy policy, ILogger<WebSocketClientBase<BinanceTickDto, Tick>> logger) : WebSocketClientBase<BinanceTickDto, Tick>(connector, receiver, processor, policy, logger)
{
    protected override Uri GetUri()
    {
        throw new NotImplementedException();
    }
}