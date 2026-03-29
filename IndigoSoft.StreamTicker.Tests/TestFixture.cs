using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IndigoSoft.StreamTicker.Tests;

public class TestFixture : IDisposable
{
    public ServiceProvider Provider { get; }

    public TestFixture()
    {
        var services = new ServiceCollection();

        // SQLite
        services.AddScoped(_ =>
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<TickDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new TickDbContext(options);
            context.Database.EnsureCreated();

            return context;
        });

        // Реальные сервисы
        services.AddSingleton<ITickRepository, TickRepository>();
        services.AddSingleton<IDeduplicator, SlidingWindowDeduplicator>();
        services.AddSingleton<IMetricsService, MetricsService>();

        services.AddLogging();

        Provider = services.BuildServiceProvider();

        // создаём БД
        using var scope = Provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.TickDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Provider.Dispose();
    }
    
    public static TickDbContext CreateDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TickDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TickDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}