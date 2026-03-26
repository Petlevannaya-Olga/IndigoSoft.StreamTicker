using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.Policies;

public class KrakenWebSocketPolicy(ILogger<KrakenWebSocketPolicy> logger) : BaseWebSocketPolicy(logger)
{
    protected override string ExchangeName => "Kraken";
}