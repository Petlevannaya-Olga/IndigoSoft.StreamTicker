using IndigoSoft.StreamTicker.Application;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;

namespace IndigoSoft.StreamTicker.Infrastructure.Policies;

public class WebSocketPolicy(ILogger<WebSocketPolicy> logger) : IWebSocketPolicy
{
    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        // Circuit breaker: прерываем после 5 ошибок в течение 30 секунд
        var circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, breakDelay) => logger.LogWarning("Circuit broken for {Delay}s", breakDelay.TotalSeconds),
                onReset: () => logger.LogInformation("Circuit reset"),
                onHalfOpen: () => logger.LogInformation("Circuit half-open")
            );

        // Retry: пробуем заново 3 раза с экспоненциальной задержкой
        var retry = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (ex, delay) => logger.LogWarning("Retrying in {Delay}s", delay.TotalSeconds)
            );

        // Timeout: не даём выполняться дольше 10 секунд
        var timeout = Policy.TimeoutAsync(
            TimeSpan.FromSeconds(10),
            TimeoutStrategy.Pessimistic,
            onTimeoutAsync: (_, timespan, _) => 
            {
                logger.LogWarning("Timeout after {Seconds}s", timespan.TotalSeconds);
                return Task.CompletedTask;
            });

        // Bulkhead: максимум 5 одновременных WebSocket-операций
        var bulkhead = Policy.BulkheadAsync(
            maxParallelization: 5,
            maxQueuingActions: 10,
            onBulkheadRejectedAsync: ctx =>
            {
                logger.LogWarning("Bulkhead rejected execution");
                return Task.CompletedTask;
            });

        // Комбинировать политики: Retry → CircuitBreaker → Timeout → Bulkhead
        var policy = Policy.WrapAsync(bulkhead, timeout, circuitBreaker, retry);
        
        await policy.ExecuteAsync(action, ct);
    }
}