using System.Net.WebSockets;

namespace IndigoSoft.StreamTicker.Application;

public interface IWebSocketConnector
{
    Task<ClientWebSocket> ConnectAsync(Uri uri, CancellationToken ct);
}
