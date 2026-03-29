using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Application;

public interface ITickRepository
{
    Task SaveBatchAsync(IEnumerable<Tick> ticks, CancellationToken ct);
    Task SaveBatchAsync(Tick[] ticks, int count, CancellationToken ct);
}