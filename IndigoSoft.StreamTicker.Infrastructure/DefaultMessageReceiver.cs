using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using IndigoSoft.StreamTicker.Application;
using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class DefaultMessageReceiver(ILogger<DefaultMessageReceiver> logger) : IMessageReceiver
{
    private const int BufferSize = 16 * 1024;
    private const int MaxMessageSize = 1024 * 1024; // 1 MB limit

    public async Task ReceiveAsync(ClientWebSocket ws, Func<string, Task> onMessage, CancellationToken ct)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var (success, msg) = await TryReadMessageAsync(ws, buffer, ct);

                if (!success)
                {
                    // соединение закрылось корректно
                    break;
                }

                if (string.IsNullOrEmpty(msg)) continue;
                try
                {
                    await onMessage(msg);
                }
                catch (Exception ex)
                {
                    var preview = msg.Length > 200 ? msg[..200] : msg;

                    logger.LogError(
                        ex,
                        "Error while processing WebSocket message. Length={Length}. Preview={Preview}",
                        msg.Length,
                        preview
                    );
                }
            }
        }
        finally
        {
            Array.Clear(buffer, 0, buffer.Length);
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task<(bool success, string? message)> TryReadMessageAsync(
        ClientWebSocket ws,
        byte[] buffer,
        CancellationToken ct)
    {
        var totalBytes = 0;

        while (true)
        {
            if (totalBytes >= buffer.Length)
                throw new InvalidOperationException("Buffer overflow while reading WebSocket message");

            WebSocketReceiveResult result;

            try
            {
                result = await ws.ReceiveAsync(
                    new ArraySegment<byte>(buffer, totalBytes, buffer.Length - totalBytes),
                    ct);
            }
            catch (WebSocketException ex)
            {
                logger.LogWarning(ex, "WebSocket receive failed");
                return (false, null);
            }

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

                logger.LogInformation("WebSocket closed by server");
                return (false, null);
            }

            if (result.MessageType == WebSocketMessageType.Binary)
            {
                logger.LogWarning("Received binary WebSocket message, ignored");
                return (true, null);
            }

            totalBytes += result.Count;

            if (totalBytes > MaxMessageSize)
                throw new InvalidOperationException("Message too large");

            if (result.EndOfMessage)
                break;
        }

        var message = Encoding.UTF8.GetString(buffer, 0, totalBytes);

        return (true, message);
    }
}