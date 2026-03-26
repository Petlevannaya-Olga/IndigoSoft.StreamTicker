namespace IndigoSoft.StreamTicker.Application;

public interface IMetricsService
{
    void Increment();
    int GetAndReset();
}