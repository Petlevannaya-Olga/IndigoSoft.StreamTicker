using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class TickDbContextFactory : IDesignTimeDbContextFactory<TickDbContext>
{
    public TickDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TickDbContext>();

        optionsBuilder.UseSqlite("Data Source=ticks.db");

        return new TickDbContext(optionsBuilder.Options);
    }
}