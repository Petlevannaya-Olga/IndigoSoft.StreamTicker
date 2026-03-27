using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using IndigoSoft.StreamTicker.Application;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class KrakenWebSocketConnector(string[] symbols, ILogger<KrakenWebSocketConnector> logger) : IWebSocketConnector
{
    public async Task<ClientWebSocket> ConnectAsync(Uri uri, CancellationToken ct)
    {
        var ws = new ClientWebSocket();
        await ws.ConnectAsync(uri, ct);
        logger.LogInformation("Connected to {Uri}", uri);
        return ws;
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