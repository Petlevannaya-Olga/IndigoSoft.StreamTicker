using System.Collections.Concurrent;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class SlidingWindowDeduplicator : IDeduplicator
{
    private readonly ConcurrentDictionary<int, byte> _set = new();
    private readonly int[] _ringBuffer;
    private int _index = -1;
    private readonly int _windowSize;

    public SlidingWindowDeduplicator(int windowSize = 5000)
    {
        _windowSize = windowSize;
        _ringBuffer = new int[windowSize];

        // инициализируем массив
        for (var i = 0; i < windowSize; i++)
            _ringBuffer[i] = 0;
    }

    public bool IsDuplicate(Tick tick)
    {
        var key = GetHashKey(tick);

        if (!_set.TryAdd(key, 0))
            return true;

        // получаем следующий индекс
        var index = Interlocked.Increment(ref _index) % _windowSize;

        // вытесняем старое значение
        var oldKey = Interlocked.Exchange(ref _ringBuffer[index], key);

        // удаляем старый элемент из set
        if (oldKey != 0)
        {
            _set.TryRemove(oldKey, out _);
        }

        return false;
    }

    private static int GetHashKey(Tick tick)
    {
        return HashCode.Combine(
            tick.Symbol,
            tick.Price,
            tick.Volume,
            tick.EventTime
        );
    }
}