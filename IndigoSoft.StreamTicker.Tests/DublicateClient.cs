using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Tests;

public class DuplicateClient(int count = 100) : IWebSocketClient<ITargetBlock<Tick>>
{
    private readonly Tick _duplicateTick = new("Exchange", "AAPL", 100, 10, 1);

    public async Task RunAsync(ITargetBlock<Tick> target, CancellationToken ct)
    {
        for (var i = 0; i < count; i++)
        {
            if (ct.IsCancellationRequested)
                break;

            // можно немного "размазать" отправку
            if (i % 10 == 0)
                await Task.Yield();

            await target.SendAsync(_duplicateTick, ct);
        }
    }
}