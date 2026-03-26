using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class TickRepository : ITickRepository
{
    private readonly TickDbContext _dbContext;

    public TickRepository(TickDbContext dbContext)
    {
        _dbContext = dbContext;
        // создаём БД и таблицу при старте
        _dbContext.Database.EnsureCreated();
    }

    public async Task SaveBatchAsync(IEnumerable<Tick> ticks, CancellationToken ct)
    {
        var list = ticks.ToList();
        if (list.Count == 0) return;

        await _dbContext.Ticks.AddRangeAsync(list, ct);
        await _dbContext.SaveChangesAsync(ct);
    }
}