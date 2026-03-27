using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Application;

public interface IWebSocketClient
{
    Task RunAsync(ITargetBlock<Tick> writer, CancellationToken ct);
}