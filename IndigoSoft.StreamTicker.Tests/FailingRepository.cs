using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Tests;

public class FailingRepository(ITickRepository inner, int failAfter = 10) : ITickRepository
{
    private int _counter;

    public async Task SaveBatchAsync(IEnumerable<Tick> ticks, CancellationToken ct)
    {
        _counter++;

        if (_counter >= failAfter)
        {
            throw new Exception("Simulated database failure");
        }

        await inner.SaveBatchAsync(ticks, ct);
    }
}