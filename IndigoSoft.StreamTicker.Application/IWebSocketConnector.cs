using System.Net.WebSockets;

namespace IndigoSoft.StreamTicker.Application;

public interface IWebSocketConnector
{
    Task<IWebSocketConnection> ConnectAsync(CancellationToken ct);
}
