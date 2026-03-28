using IndigoSoft.StreamTicker.Contracts;

namespace IndigoSoft.StreamTicker.Application;

public interface IMetricsService
{
    void IncrementIn();
    void IncrementOut();
    void IncrementDeduplicated();
    void IncrementBatch();

    MetricsSnapshot GetAndReset();
}