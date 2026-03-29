using FsCheck.Xunit;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure;

namespace IndigoSoft.StreamTicker.Tests.Properties;

public class SlidingWindowDeduplicatorProperties
{
    private static bool IsValid(double value)
        => !double.IsNaN(value) && !double.IsInfinity(value);

    // 1. Никогда не падает
    [Property]
    public bool Should_Never_Throw(
        string symbol,
        double price,
        double volume,
        long eventTime)
    {
        if (!IsValid(price) || !IsValid(volume))
            return true;

        var deduplicator = new SlidingWindowDeduplicator();

        var tick = new Tick("Exchange", symbol, price, volume, eventTime);

        deduplicator.IsDuplicate(tick);

        return true;
    }

    // 2. Один и тот же тик → второй раз всегда duplicate
    [Property]
    public bool Same_Tick_Should_Be_Duplicate_Second_Time(
        string symbol,
        double price,
        double volume,
        long eventTime)
    {
        if (!IsValid(price) || !IsValid(volume))
            return true;

        var deduplicator = new SlidingWindowDeduplicator();

        var tick = new Tick("Exchange", symbol, price, volume, eventTime);

        var first = deduplicator.IsDuplicate(tick);
        var second = deduplicator.IsDuplicate(tick);

        return first == false && second == true;
    }

    // 4. Разные инстансы не должны зависеть друг от друга
    [Property]
    public bool Different_Instances_Should_Not_Share_State(
        string symbol,
        double price,
        double volume,
        long eventTime)
    {
        if (!IsValid(price) || !IsValid(volume))
            return true;

        var tick = new Tick("Exchange", symbol, price, volume, eventTime);

        var d1 = new SlidingWindowDeduplicator();
        var d2 = new SlidingWindowDeduplicator();

        var r1 = d1.IsDuplicate(tick);
        var r2 = d2.IsDuplicate(tick);

        return r1 == r2;
    }

    // 5. Parallel safety
    [Property]
    public bool Should_Not_Crash_In_Parallel()
    {
        var deduplicator = new SlidingWindowDeduplicator();

        Parallel.For(0, 1000, i =>
        {
            var tick = new Tick("Exchange", "SYM", i, i, i);
            deduplicator.IsDuplicate(tick);
        });

        return true;
    }

    // 6. Поведение при переполнении окна (инвариант: система не ломается)
    [Property]
    public bool Window_Should_Handle_Overflow_Gracefully(
        string symbol,
        double price,
        double volume,
        long t1,
        long t2,
        long t3,
        long t4)
    {
        if (!IsValid(price) || !IsValid(volume))
            return true;

        var deduplicator = new SlidingWindowDeduplicator(2);

        var ticks = new[]
        {
            new Tick("Exchange", symbol, price, volume, t1),
            new Tick("Exchange", symbol, price, volume, t2),
            new Tick("Exchange", symbol, price, volume, t3),
            new Tick("Exchange", symbol, price, volume, t4)
        };

        foreach (var tick in ticks)
            deduplicator.IsDuplicate(tick);

        // повторная обработка — система должна оставаться стабильной
        foreach (var tick in ticks)
            deduplicator.IsDuplicate(tick);

        return true;
    }

    // 7. Реальный ключевой property: стабильность результата
    [Property]
    public bool Same_Tick_Always_Produces_Same_Result_Order(
        string symbol,
        double price,
        double volume,
        long eventTime)
    {
        if (!IsValid(price) || !IsValid(volume))
            return true;

        var deduplicator = new SlidingWindowDeduplicator();

        var tick = new Tick("Exchange", symbol, price, volume, eventTime);

        var r1 = deduplicator.IsDuplicate(tick);
        var r2 = deduplicator.IsDuplicate(tick);

        return r1 == false && r2 == true;
    }

    // 8. Грубая защита от мусорных значений
    [Property]
    public bool Should_Handle_Invalid_Doubles_Gracefully(
        string symbol,
        long eventTime)
    {
        var deduplicator = new SlidingWindowDeduplicator();

        var tick = new Tick("Exchange", symbol, double.NaN, double.NaN, eventTime);

        deduplicator.IsDuplicate(tick);

        return true;
    }
}