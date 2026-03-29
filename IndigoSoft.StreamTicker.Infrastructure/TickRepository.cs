using EFCore.BulkExtensions;
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
    
    public async Task SaveBatchAsync(Tick[] ticks, int count, CancellationToken ct)
    {
        if (count == 0)
            return;

        await _dbContext.BulkInsertAsync(ticks.Take(count).ToList(), cancellationToken: ct);
    }
}