namespace IndigoSoft.StreamTicker.Application;

public interface IParser<T>
{
    List<T>? Parse(string json);
}