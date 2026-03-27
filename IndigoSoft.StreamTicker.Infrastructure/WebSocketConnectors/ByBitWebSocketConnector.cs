using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using IndigoSoft.StreamTicker.Application;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketConnectors;

public class ByBitWebSocketConnector(string[] symbols, ILogger<ByBitWebSocketConnector> logger) : IWebSocketConnector
{
    public async Task<ClientWebSocket> ConnectAsync(Uri uri, CancellationToken ct)
    {
        var ws = new ClientWebSocket();
        await ws.ConnectAsync(uri, ct);
        logger.LogInformation("Connected to {Uri}", uri);
        await SubscribeAsync(ws, ct);
        return ws;
    }
    
    private async Task SubscribeAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var subscribe = new
        {
            op = "subscribe",
            args = symbols.Select(s => $"tickers.{s}").ToArray()
        };

        var json = JsonSerializer.Serialize(subscribe);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }
}