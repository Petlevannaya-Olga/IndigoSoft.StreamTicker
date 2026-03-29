using IndigoSoft.StreamTicker.Application;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;

namespace IndigoSoft.StreamTicker.Infrastructure.Policies;

public class WebSocketPolicy : IWebSocketPolicy
{
    private readonly IAsyncPolicy _policy;

    public WebSocketPolicy(ILogger<WebSocketPolicy> logger)
    {
        // Circuit breaker: разрываем цепь после 5 ошибок подряд и не даём выполнять операции 30 секунд
        var circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, breakDelay) =>
                    logger.LogWarning("Circuit broken for {Delay}s", breakDelay.TotalSeconds),
                onReset: () =>
                    logger.LogInformation("Circuit reset"),
                onHalfOpen: () =>
                    logger.LogInformation("Circuit half-open")
            );

        // Retry: бесконечные попытки переподключения с экспоненциальной задержкой + декоррелированный jitter
        var retry = Policy
            .Handle<Exception>()
            .WaitAndRetryForeverAsync(
                sleepDurationProvider: attempt =>
                {
                    // Экспоненциальный рост задержки (2, 4, 8, 16, ...)
                    var baseDelay = Math.Min(30, Math.Pow(2, attempt));

                    // Декоррелированный jitter
                    var delay = Random.Shared.Next(
                        (int)baseDelay,
                        (int)(baseDelay * 3)
                    );

                    return TimeSpan.FromSeconds(delay);
                },
                onRetry: (ex, delay) =>
                    logger.LogWarning("Retrying in {Delay}s", delay.TotalSeconds)
            );

        // Timeout: ограничиваем длительность одной попытки (например, зависший сокет)
        var timeout = Policy.TimeoutAsync(
            timeout: TimeSpan.FromSeconds(10),
            timeoutStrategy: TimeoutStrategy.Pessimistic,
            onTimeoutAsync: (_, timespan, _) =>
            {
                logger.LogWarning("Timeout after {Seconds}s", timespan.TotalSeconds);
                return Task.CompletedTask;
            });

        // Bulkhead: ограничиваем количество одновременных операций
        // защищает от перегрузки при массовых реконнектах
        var bulkhead = Policy.BulkheadAsync(
            maxParallelization: 5,
            maxQueuingActions: 10,
            onBulkheadRejectedAsync: _ =>
            {
                logger.LogWarning("Bulkhead rejected execution");
                return Task.CompletedTask;
            });

        _policy = Policy.WrapAsync(
            retry,  // Retry снаружи!
            circuitBreaker,
            timeout,
            bulkhead
        );
    }

    public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        return _policy.ExecuteAsync(action, ct);
    }
}