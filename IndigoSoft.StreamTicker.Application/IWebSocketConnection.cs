using System.Net.WebSockets;

namespace IndigoSoft.StreamTicker.Application;

public interface IWebSocketConnection
{
    Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken ct);
    Task CloseAsync(WebSocketCloseStatus status, string description, CancellationToken ct);
}