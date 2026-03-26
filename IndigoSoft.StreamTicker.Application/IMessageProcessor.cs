namespace IndigoSoft.StreamTicker.Application;

public interface IMessageProcessor<TDomain> where TDomain : class
{
    Task<TDomain?> ProcessMessageAsync(string message, CancellationToken ct);
}