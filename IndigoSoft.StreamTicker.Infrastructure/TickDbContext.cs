using IndigoSoft.StreamTicker.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class TickDbContext(DbContextOptions<TickDbContext> options) : DbContext(options)
{
    public DbSet<Tick> Ticks => Set<Tick>();
}