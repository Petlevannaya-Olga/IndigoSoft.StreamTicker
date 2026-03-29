using System.Net.WebSockets;
using IndigoSoft.StreamTicker.Application;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class ClientWebSocketAdapter(ClientWebSocket ws) : IWebSocketConnection
{
    public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken ct)
        => ws.ReceiveAsync(buffer, ct);

    public Task CloseAsync(WebSocketCloseStatus status, string description, CancellationToken ct)
        => ws.CloseAsync(status, description, ct);
}