using IndigoSoft.StreamTicker.Application.Policies;
using Microsoft.Extensions.Logging;
using Polly;

namespace IndigoSoft.StreamTicker.Infrastructure.Policies;

public abstract class BaseWebSocketPolicy(ILogger<BaseWebSocketPolicy> logger) : IWebSocketPolicy
{
    protected readonly Random Random = new();

    protected abstract string ExchangeName { get; }

    public virtual IAsyncPolicy GetRetryPolicy() => Policy
        .Handle<Exception>()
        .WaitAndRetryForeverAsync(
            attempt => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 30))
                       + TimeSpan.FromMilliseconds(Random.Next(0, 500)),
            (ex, ts, ctx) => logger.LogError("[{ExchangeName}] Retry: {Error}", ExchangeName, ex.Message)
        );

    public virtual IAsyncPolicy GetCircuitBreakerPolicy() => Policy
        .Handle<Exception>()
        .CircuitBreakerAsync(
            3, TimeSpan.FromSeconds(15),
            onBreak: (ex, ts) => logger.LogError("[{ExchangeName}] Circuit открыт", ExchangeName),
            onReset: () => logger.LogError("[{ExchangeName}] Circuit закрыт", ExchangeName)
        );

    public virtual IAsyncPolicy GetTimeoutPolicy() => Policy.TimeoutAsync(TimeSpan.FromSeconds(10));

    public virtual IAsyncPolicy GetBulkheadPolicy() => Policy.BulkheadAsync(
        maxParallelization: 5,
        maxQueuingActions: 10,
        onBulkheadRejectedAsync: _ =>
        {
            logger.LogError("[{ExchangeName}] Bulkhead превышен", ExchangeName);
            return Task.CompletedTask;
        }
    );

    public virtual IAsyncPolicy GetRateLimitPolicy() =>
        Policy.RateLimitAsync(50, TimeSpan.FromSeconds(1));

    public virtual IAsyncPolicy WrapWebSocketPolicy() =>
        Policy.WrapAsync(
            GetRetryPolicy(),
            GetCircuitBreakerPolicy(),
            GetTimeoutPolicy(),
            GetBulkheadPolicy());

    public virtual IAsyncPolicy WrapRestApiPolicy() =>
        Policy.WrapAsync(
            GetRetryPolicy(),
            GetCircuitBreakerPolicy(),
            GetTimeoutPolicy(),
            GetRateLimitPolicy());
}