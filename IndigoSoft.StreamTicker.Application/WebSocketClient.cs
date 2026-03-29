namespace IndigoSoft.StreamTicker.Application;

public interface IWebSocketClient<T>
{
    Task RunAsync(T writer, CancellationToken ct);
}