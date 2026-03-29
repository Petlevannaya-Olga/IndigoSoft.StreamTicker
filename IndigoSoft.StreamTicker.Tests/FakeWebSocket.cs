using System.Net.WebSockets;
using System.Text;
using IndigoSoft.StreamTicker.Application;

namespace IndigoSoft.StreamTicker.Tests;

public class FakeWebSocket : IWebSocketConnection
{
    private class Message
    {
        public byte[] Data = null!;
        public WebSocketMessageType Type;
        public int Offset;
    }

    private readonly Queue<Message> _messages = new();

    private bool _throwOnNext;
    private bool _closeNext;

    public void Enqueue(string message)
    {
        _messages.Enqueue(new Message
        {
            Data = Encoding.UTF8.GetBytes(message),
            Type = WebSocketMessageType.Text
        });
    }

    public void EnqueueBinary(byte[] data)
    {
        _messages.Enqueue(new Message
        {
            Data = data,
            Type = WebSocketMessageType.Binary
        });
    }

    public void ThrowOnNextReceive() => _throwOnNext = true;

    public void CloseNext() => _closeNext = true;

    public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken ct)
    {
        if (_throwOnNext)
        {
            _throwOnNext = false;
            throw new WebSocketException();
        }

        if (_closeNext)
        {
            _closeNext = false;

            return Task.FromResult(new WebSocketReceiveResult(
                0,
                WebSocketMessageType.Close,
                true));
        }

        if (_messages.Count == 0)
        {
            return Task.FromResult(new WebSocketReceiveResult(
                0,
                WebSocketMessageType.Close,
                true));
        }

        var msg = _messages.Peek();

        var remaining = msg.Data.Length - msg.Offset;
        var count = Math.Min(buffer.Count, remaining);

        Array.Copy(msg.Data, msg.Offset, buffer.Array!, buffer.Offset, count);

        msg.Offset += count;

        var endOfMessage = msg.Offset >= msg.Data.Length;

        if (endOfMessage)
            _messages.Dequeue();

        return Task.FromResult(new WebSocketReceiveResult(
            count,
            msg.Type,
            endOfMessage));
    }

    public Task CloseAsync(WebSocketCloseStatus status, string description, CancellationToken ct)
        => Task.CompletedTask;
}