using Microsoft.Extensions.Logging;

namespace IndigoSoft.StreamTicker.Infrastructure.Policies;

public class KrakenPolicies(ILogger<KrakenPolicies> logger) : BasePolicies(logger)
{
    protected override string ExchangeName => "Kraken";
}