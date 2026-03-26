namespace IndigoSoft.StreamTicker.Application;

public interface IDeduplicator<in T>
{
    bool IsDuplicate(T item);
}