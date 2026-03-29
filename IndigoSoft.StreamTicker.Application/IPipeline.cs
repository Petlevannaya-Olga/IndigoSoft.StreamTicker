namespace IndigoSoft.StreamTicker.Application;

public interface IPipeline
{
    Task RunAsync(CancellationToken ct);
}