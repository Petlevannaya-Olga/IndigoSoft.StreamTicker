using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Application;

public interface IDeduplicator
{
    bool IsDuplicate(Tick item);
}