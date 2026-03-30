namespace IndigoSoft.StreamTicker.Application;

public interface IWebSocketClient<in TTarget>
{
    Task RunAsync(TTarget target, CancellationToken ct);
}