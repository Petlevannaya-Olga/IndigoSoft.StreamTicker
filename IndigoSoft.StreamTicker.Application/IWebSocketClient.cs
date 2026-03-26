using System.Threading.Channels;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Application;

public interface IWebSocketClient
{
    Task RunAsync(ChannelWriter<Tick> writer, CancellationToken ct);
}