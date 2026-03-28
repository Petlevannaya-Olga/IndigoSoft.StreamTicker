using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Tests.Integrations;

public class TestWebSocketClient(int ticks) : IWebSocketClient
{
    public async Task RunAsync(ITargetBlock<Tick> target, CancellationToken ct)
    {
        for (int i = 0; i < ticks; i++)
        {
            await target.SendAsync(
                new Tick("Exchange", "AAPL", 100, 10, i),
                ct);
        }

        target.Complete();
    }
}