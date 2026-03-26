using IndigoSoft.StreamTicker.Application;

namespace IndigoSoft.StreamTicker.Infrastructure;

public class MetricsService : IMetricsService
{
    private int _counter;

    /// <summary>
    /// Увеличивает счётчик тиксов (TPS)
    /// </summary>
    public void Increment()
    {
        Interlocked.Increment(ref _counter);
    }

    /// <summary>
    /// Возвращает текущее значение и обнуляет счётчик
    /// </summary>
    public int GetAndReset()
    {
        return Interlocked.Exchange(ref _counter, 0);
    }
}