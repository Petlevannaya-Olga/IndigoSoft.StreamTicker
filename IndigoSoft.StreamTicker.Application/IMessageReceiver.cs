using System.Net.WebSockets;

namespace IndigoSoft.StreamTicker.Application;

public interface IMessageReceiver
{
    Task ReceiveAsync(IWebSocketConnection ws, Func<string, Task> onMessage, CancellationToken ct);
}