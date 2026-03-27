namespace IndigoSoft.StreamTicker.Application;

public interface IMessageProcessor<out TDomain> where TDomain : class
{
    TDomain? ProcessMessage(string message, CancellationToken ct);
}