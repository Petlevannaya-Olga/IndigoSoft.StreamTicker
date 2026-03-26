using Microsoft.Extensions.Logging;
using Polly;

namespace IndigoSoft.StreamTicker.Infrastructure.Policies;

public class BinancePolicies(ILogger<BinancePolicies> logger) : BasePolicies(logger)
{
    protected override string ExchangeName => "Binance";

    public override IAsyncPolicy GetRetryPolicy()
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryForeverAsync(
                attempt => TimeSpan.FromSeconds(Math.Min(Math.Pow(1.5, attempt), 20))
                           + TimeSpan.FromMilliseconds(Random.Next(0, 300)),
                (ex, ts, ctx) => logger.LogError("[{ExchangeName}] Retry in {Ts:F2}s | Error: {Error}", ExchangeName, ts, ex.Message));
    }
}