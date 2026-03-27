using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class BinanceWebSocketClient(
    IEnumerable<string> symbols,
    IWebSocketConnector connector,
    IMessageReceiver receiver,
    IMessageProcessor<Tick> processor,
    IWebSocketPolicy policy,
    ILogger<BinanceWebSocketClient> logger) :
    WebSocketClientBase<BinanceTickDto, Tick>(connector, receiver, processor, policy, logger)
{
    protected override Uri GetUri()
    {
        return new Uri($"wss://stream.binance.com:9443/stream?streams={string.Join("@trade/", symbols).ToLower()}@trade");
    }
}