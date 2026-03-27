using System.Net.WebSockets;
using IndigoSoft.StreamTicker.Application;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class KrakenWebSocketConnector( ILogger<KrakenWebSocketConnector> logger) : IWebSocketConnector
{
    public async Task<ClientWebSocket> ConnectAsync(Uri uri, CancellationToken ct)
    {
        var ws = new ClientWebSocket();
        await ws.ConnectAsync(uri, ct);
        logger.LogInformation("Connected to {Uri}", uri);
        return ws;
    }
}