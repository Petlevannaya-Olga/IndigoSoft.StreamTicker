using System.Threading.Tasks.Dataflow;

namespace IndigoSoft.StreamTicker.Application;

public interface IWebSocketClient<out T>
{
    Task RunAsync(ITargetBlock<T> writer, CancellationToken ct);
}