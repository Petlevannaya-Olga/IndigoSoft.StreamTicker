using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using IndigoSoft.StreamTicker.Application;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketConnectors;

public class KrakenWebSocketConnector(Uri uri, string[] symbols, ILogger<KrakenWebSocketConnector> logger) : IWebSocketConnector
{
    public async Task<IWebSocketConnection> ConnectAsync(CancellationToken ct)
    {
        var ws = new ClientWebSocket();
        await ws.ConnectAsync(uri, ct);
        logger.LogInformation("Connected to {Uri}", uri);
        await SubscribeAsync(ws, ct);
        return new ClientWebSocketAdapter(ws);
    }

    private async Task SubscribeAsync(ClientWebSocket ws, CancellationToken ct)
    {
        // Подписка на сделки для нескольких пар
        var subscribeMsg = new
        {
            @event = "subscribe",
            pair = symbols,
            subscription = new { name = "trade" }
        };

        await ws.SendAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(subscribeMsg)),
            WebSocketMessageType.Text,
            true,
            ct);
    }
}