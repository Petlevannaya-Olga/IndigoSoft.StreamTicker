using System.Collections.Concurrent;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class SlidingWindowDeduplicator<T>(int windowSize = 5000) : IDeduplicator<Tick>
{
    private readonly ConcurrentQueue<int> _queue = new();
    private readonly ConcurrentDictionary<int, byte> _set = new();

    public bool IsDuplicate(Tick tick)
    {
        var keyHash = GetHashKey(tick);

        if (!_set.TryAdd(keyHash, 0)) return true;

        _queue.Enqueue(keyHash);
        
        if (_queue.Count > windowSize && _queue.TryDequeue(out var oldKey))
            _set.TryRemove(oldKey, out _);

        return false;
    }

    private static int GetHashKey(Tick tick)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + tick.Symbol.GetHashCode();
            hash = hash * 31 + tick.Price.GetHashCode();
            hash = hash * 31 + tick.Volume.GetHashCode();
            hash = hash * 31 + tick.EventTime.GetHashCode();
            return hash;
        }
    }
}