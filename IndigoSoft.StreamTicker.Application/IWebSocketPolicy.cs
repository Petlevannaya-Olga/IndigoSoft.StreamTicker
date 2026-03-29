namespace IndigoSoft.StreamTicker.Application;

public interface IWebSocketPolicy
{
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct);
}