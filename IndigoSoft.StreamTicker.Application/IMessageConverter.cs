using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Application;

public interface IMessageConverter
{
    List<Tick>? Convert(string message, CancellationToken ct);
}