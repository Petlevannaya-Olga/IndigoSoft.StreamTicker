using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Application;

public interface ITickRepository
{
    Task SaveBatchAsync(IEnumerable<Tick> ticks, CancellationToken ct);
}