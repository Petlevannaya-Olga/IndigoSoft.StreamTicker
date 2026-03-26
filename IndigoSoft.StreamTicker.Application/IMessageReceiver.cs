using System.Net.WebSockets;

namespace IndigoSoft.StreamTicker.Application;

public interface IMessageReceiver
{
    Task ReceiveAsync(ClientWebSocket ws, Func<string, Task> onMessage, CancellationToken ct);
}