using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Tests;

public class TestDataflowWebSocketClient(int ticks) : IWebSocketClient<ITargetBlock<Tick>>
{
    public async Task RunAsync(ITargetBlock<Tick> target, CancellationToken ct)
    {
        for (var i = 0; i < ticks; i++)
        {
            var accepted = await target.SendAsync(
                new Tick("Exchange", "AAPL", 100, 10, i),
                ct);

            if (!accepted)
            {
                throw new Exception("Tick was not accepted by pipeline");
            }
        }
    }
}