namespace IndigoSoft.StreamTicker.Application;

public interface IMessageConverter<T>
{
    List<T>? Convert(string message, CancellationToken ct);
}