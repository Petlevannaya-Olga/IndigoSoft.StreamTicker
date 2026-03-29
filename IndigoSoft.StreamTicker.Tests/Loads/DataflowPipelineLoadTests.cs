using System.Diagnostics;
using FluentAssertions;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Infrastructure;
using Microsoft.Extensions.Logging;
using TickDbContext = IndigoSoft.StreamTicker.Infrastructure.TickDbContext;

namespace IndigoSoft.StreamTicker.Tests.Loads;

[Trait("Category", "Load")]
public class DataflowPipelineLoadTests(TestFixture fixture) : TestBase(fixture)
{
    [Fact]
    public async Task Should_handle_100k_ticks()
    {
        // Arrange
        var repository = Get<ITickRepository>();
        var deduplicator = Get<IDeduplicator>();
        var metrics = Get<IMetricsService>();
        var logger = Get<ILogger<DataflowPipeline>>();

        var ticksCount = 100_000;

        var client = new LoadWebSocketClient(ticksCount);

        var pipeline = new DataflowPipeline(
            [client],
            repository,
            deduplicator,
            metrics,
            logger);

        var sw = Stopwatch.StartNew();

        // Act
        await pipeline.RunAsync(CancellationToken.None);

        sw.Stop();

        // Assert
        var db = Get<TickDbContext>();

        db.Ticks.Count().Should().BeGreaterThan(0);

        var ticksPerSecond = ticksCount / sw.Elapsed.TotalSeconds;

        ticksPerSecond.Should().BeGreaterThan(10_000);
    }
}