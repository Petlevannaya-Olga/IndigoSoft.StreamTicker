using FluentAssertions;
using FsCheck.Xunit;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace IndigoSoft.StreamTicker.Tests.Properties;

public class PipelineProperties(TestFixture fixture) : TestBase(fixture)
{
    [Property]
    public async Task Pipeline_should_always_complete(int tickCount)
    {
        tickCount = Math.Abs(tickCount % 1000); // ограничим диапазон

        var client = new TestWebSocketClient(tickCount);

        var pipeline = new Pipeline(
            [client],
            Get<ITickRepository>(),
            Get<IDeduplicator>(),
            Get<IMetricsService>(),
            Get<ILogger<Pipeline>>());

        var task = pipeline.RunAsync(CancellationToken.None);

        var completed = await Task.WhenAny(task, Task.Delay(5000));

        completed.Should().Be(task);

        await task;
    }

    [Property]
    public async Task No_more_than_unique_ticks_are_saved(int tickCount)
    {
        tickCount = Math.Abs(tickCount % 1000);

        var client = new DuplicateClient(); // специально генерит дубликаты

        var pipeline = new Pipeline(
            [client],
            Get<ITickRepository>(),
            Get<IDeduplicator>(),
            Get<IMetricsService>(),
            Get<ILogger<Pipeline>>());

        await pipeline.RunAsync(CancellationToken.None);

        var db = Get<TickDbContext>();

        var total = db.Ticks.Count();
        var unique = db.Ticks
            .GroupBy(t => new { t.Symbol, t.Price, t.Volume, t.EventTime })
            .Count();

        total.Should().Be(unique);
    }

    [Property]
    public async Task Batch_size_should_not_exceed_limit(int tickCount)
    {
        tickCount = Math.Abs(tickCount % 5000);

        var savedBatches = new List<Tick[]>();

        var repository = new Mock<ITickRepository>();

        repository
            .Setup(r => r.SaveBatchAsync(It.IsAny<IEnumerable<Tick>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<IEnumerable<Tick>, CancellationToken>((batch, _) => { savedBatches.Add(batch.ToArray()); });

        var client = new TestWebSocketClient(tickCount);

        var pipeline = new Pipeline(
            [client],
            repository.Object,
            Get<IDeduplicator>(),
            Get<IMetricsService>(),
            Get<ILogger<Pipeline>>());

        await pipeline.RunAsync(CancellationToken.None);

        if (tickCount == 0)
        {
            savedBatches.Should().BeEmpty();
            return;
        }

        // ✅ инварианты
        savedBatches.Should().OnlyContain(b => b.Length <= 2000);

        var total = savedBatches.Sum(b => b.Length);
        total.Should().BeLessThanOrEqualTo(tickCount);
    }

    [Property]
    public async Task Metrics_should_be_consistent(int tickCount)
    {
        tickCount = Math.Abs(tickCount % 500);

        var client = new TestWebSocketClient(tickCount);

        var pipeline = new Pipeline(
            [client],
            Get<ITickRepository>(),
            Get<IDeduplicator>(),
            Get<IMetricsService>(),
            Get<ILogger<Pipeline>>());

        await pipeline.RunAsync(CancellationToken.None);

        var metrics = Get<IMetricsService>().GetAndReset();

        metrics.In.Should().BeGreaterThanOrEqualTo(0);
        metrics.Out.Should().BeGreaterThanOrEqualTo(0);
        metrics.Deduplicated.Should().BeGreaterThanOrEqualTo(0);

        (metrics.Out + metrics.Deduplicated)
            .Should()
            .BeGreaterThanOrEqualTo(metrics.In);
    }
}