using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class MetricsService : IMetricsService
{
    private int _in;
    private int _out;
    private int _deduplicated;
    private int _batch;
    private int _dropped;

    public void IncrementIn()
    {
        Interlocked.Increment(ref _in);
    }

    public void IncrementOut()
    {
        Interlocked.Increment(ref _out);
    }

    public void IncrementDeduplicated()
    {
        Interlocked.Increment(ref _deduplicated);
    }

    public void IncrementBatch()
    {
        Interlocked.Increment(ref _batch);
    }
    
    public MetricsSnapshot GetAndReset()
    {
        return new MetricsSnapshot
        {
            In = Interlocked.Exchange(ref _in, 0),
            Out = Interlocked.Exchange(ref _out, 0),
            Deduplicated = Interlocked.Exchange(ref _deduplicated, 0),
            BatchCount = Volatile.Read(ref _batch),
        };
    }
}