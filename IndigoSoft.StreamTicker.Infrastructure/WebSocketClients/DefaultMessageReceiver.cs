using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using IndigoSoft.StreamTicker.Application;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class DefaultMessageReceiver : IMessageReceiver
{
    private const int BufferSize = 16 * 1024;

    public async Task ReceiveAsync(ClientWebSocket ws, Func<string, Task> onMessage, CancellationToken ct)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

        try
        {
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var (success, message) = await TryReadMessageAsync(ws, buffer, ct);
                if (!success)
                {
                    // Соединение закрылось корректно
                    break;
                }

                if (!string.IsNullOrWhiteSpace(message))
                    await onMessage(message);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<(bool success, string? message)> TryReadMessageAsync(ClientWebSocket ws, byte[] buffer, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed by server", ct);
                }
                catch
                {
                    // ignored
                }

                return (false, null);
            }

            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        var message = Encoding.UTF8.GetString(ms.ToArray());
        return (true, message);
    }
}