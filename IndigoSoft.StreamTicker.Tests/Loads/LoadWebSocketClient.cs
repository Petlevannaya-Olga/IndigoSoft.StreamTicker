using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Tests.Loads;

public class LoadWebSocketClient(int ticks) : IWebSocketClient
{
    public async Task RunAsync(ITargetBlock<Tick> target, CancellationToken ct)
    {
        for (var i = 0; i < ticks; i++)
        {
            if (i % 1000 == 0)
                await Task.Yield(); // даём планировщику шанс

            await target.SendAsync(
                new Tick("Exchange", $"SYM{i % 10}", 100, 10, i),
                ct);
        }

        target.Complete();
    }
}