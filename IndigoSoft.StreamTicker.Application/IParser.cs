namespace IndigoSoft.StreamTicker.Application;

public interface IParser<out T>
{
    T? Parse(string json);
}