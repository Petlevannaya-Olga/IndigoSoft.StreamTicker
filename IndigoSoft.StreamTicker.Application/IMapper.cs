namespace IndigoSoft.StreamTicker.Application;

public interface IMapper<in TIn, out TOut>
{
    TOut Map(TIn source);
}