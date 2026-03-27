namespace IndigoSoft.StreamTicker.Application;

public interface IMessageProcessor<out TDomain> where TDomain : class
{
    IEnumerable<TDomain>? ProcessMessage(string message, CancellationToken ct);
}