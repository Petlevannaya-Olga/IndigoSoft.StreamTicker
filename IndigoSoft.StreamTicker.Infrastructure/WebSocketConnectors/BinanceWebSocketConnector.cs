using System.Net.WebSockets;
using IndigoSoft.StreamTicker.Application;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketConnectors;

public class BinanceWebSocketConnector(Uri uri, ILogger<BinanceWebSocketConnector> logger)
    : IWebSocketConnector
{
    public async Task<IWebSocketConnection> ConnectAsync(CancellationToken ct)
    {
        var ws = new ClientWebSocket();
        await ws.ConnectAsync(uri, ct);
        logger.LogInformation("Connected to {Uri}", uri);
        return new ClientWebSocketAdapter(ws);
    }
}
