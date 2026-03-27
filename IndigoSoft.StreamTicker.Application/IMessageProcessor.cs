namespace IndigoSoft.StreamTicker.Application;

public interface IMessageProcessor<out TDomain>
{
    IEnumerable<TDomain>? ProcessMessage(string message, CancellationToken ct);
}