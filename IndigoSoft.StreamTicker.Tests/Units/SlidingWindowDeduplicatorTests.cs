using System.Collections.Concurrent;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure;

namespace IndigoSoft.StreamTicker.Tests.Units;

public class SlidingWindowDeduplicatorTests
{
    private Tick CreateTick(
        string symbol = "AAPL",
        double price = 100,
        double volume = 10,
        long eventTime = 1000)
    {
        return new Tick("Exchange", symbol, price, volume, eventTime);
    }

    [Fact]
    public void Should_Treat_Ticks_With_Different_EventTime_As_Different()
    {
        var deduplicator = new SlidingWindowDeduplicator();

        var t1 = CreateTick(eventTime: 1000);
        var t2 = CreateTick(eventTime: 2000);

        Assert.False(deduplicator.IsDuplicate(t1));
        Assert.False(deduplicator.IsDuplicate(t2));
    }

    [Fact]
    public void Should_Detect_Duplicate_When_All_Fields_Equal()
    {
        var deduplicator = new SlidingWindowDeduplicator();

        var t1 = CreateTick(eventTime: 1000);
        var t2 = CreateTick(eventTime: 1000);

        var first = deduplicator.IsDuplicate(t1);
        var second = deduplicator.IsDuplicate(t2);

        Assert.False(first);
        Assert.True(second);
    }

    [Fact]
    public void Should_Distinguish_By_All_Fields()
    {
        var deduplicator = new SlidingWindowDeduplicator();

        var t1 = CreateTick(symbol: "AAPL", price: 100, volume: 10, eventTime: 1);
        var t2 = CreateTick(symbol: "AAPL", price: 101, volume: 10, eventTime: 1);

        Assert.False(deduplicator.IsDuplicate(t1));
        Assert.False(deduplicator.IsDuplicate(t2));
    }

    [Fact]
    public void Should_Treat_Slightly_Different_Ticks_As_Different()
    {
        var deduplicator = new SlidingWindowDeduplicator();

        var t1 = CreateTick(price: 100);
        var t2 = CreateTick(price: 100.000001);

        Assert.False(deduplicator.IsDuplicate(t1));
        Assert.False(deduplicator.IsDuplicate(t2));
    }

    [Fact]
    public void Should_Handle_Concurrency_With_Same_Tick()
    {
        var deduplicator = new SlidingWindowDeduplicator();

        var tick = CreateTick(eventTime: 1000);
        var results = new ConcurrentBag<bool>();

        Parallel.For(0, 1000, _ =>
        {
            results.Add(deduplicator.IsDuplicate(tick));
        });

        // хотя бы один должен быть false (первый вызов)
        Assert.Contains(results, x => x == false);
    }

    [Fact]
    public void Should_Not_Throw_Under_Heavy_Concurrency()
    {
        var deduplicator = new SlidingWindowDeduplicator();

        var ticks = Enumerable.Range(0, 1000)
            .Select(i => CreateTick(eventTime: i))
            .ToList();

        Parallel.ForEach(ticks, tick =>
        {
            deduplicator.IsDuplicate(tick);
        });

        Assert.True(true);
    }

    [Fact]
    public void Should_Handle_Window_Overflow_Without_Errors()
    {
        var deduplicator = new SlidingWindowDeduplicator(3);

        var ticks = Enumerable.Range(0, 10)
            .Select(i => CreateTick(eventTime: i))
            .ToList();

        foreach (var tick in ticks)
            deduplicator.IsDuplicate(tick);

        // повторная обработка — система должна оставаться стабильной
        foreach (var tick in ticks)
            deduplicator.IsDuplicate(tick);

        Assert.True(true);
    }

    [Fact]
    public void Should_Handle_Mixed_Data_Stream()
    {
        var deduplicator = new SlidingWindowDeduplicator(100);
        var random = new Random();

        Parallel.For(0, 1000, i =>
        {
            var tick = CreateTick(
                symbol: "SYM" + (i % 10),
                price: random.Next(0, 100),
                volume: random.Next(1, 10),
                eventTime: i % 50
            );

            deduplicator.IsDuplicate(tick);
        });

        Assert.True(true);
    }
}