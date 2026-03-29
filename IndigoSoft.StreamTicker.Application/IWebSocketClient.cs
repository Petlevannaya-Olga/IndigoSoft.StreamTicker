namespace IndigoSoft.StreamTicker.Application;

public interface IWebSocketClient<in T>
{
    Task RunAsync(T writer, CancellationToken ct);
}