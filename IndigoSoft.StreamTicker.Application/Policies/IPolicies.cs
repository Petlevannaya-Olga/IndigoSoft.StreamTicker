using Polly;

namespace IndigoSoft.StreamTicker.Application.Policies;

public interface IPolicies
{
    /// <summary>
    /// Возвращает политику повторных попыток для обработки временных ошибок.
    /// </summary>
    IAsyncPolicy GetRetryPolicy();
    
    /// <summary>
    /// Возвращает политику Circuit Breaker для предотвращения частых сбоев.
    /// </summary>
    IAsyncPolicy GetCircuitBreakerPolicy();
    
    /// <summary>
    /// Возвращает политику ограничения параллелизма (Bulkhead).
    /// </summary>
    IAsyncPolicy GetTimeoutPolicy();
    
    /// <summary>
    /// Возвращает политику ограничения параллелизма.
    /// </summary>
    IAsyncPolicy GetBulkheadPolicy();
    
    /// <summary>
    /// Возвращает политику ограничения количества запросов в единицу времени.
    /// </summary>
    IAsyncPolicy GetRateLimitPolicy();

    /// <summary>
    /// Возвращает комбинированную политику для WebSocket (Retry, CircuitBreaker, Timeout, Bulkhead).
    /// </summary>
    IAsyncPolicy WrapWebSocketPolicy();
    
    /// <summary>
    /// Возвращает комбинированную политику для REST API (Retry, CircuitBreaker, Timeout, RateLimit).
    /// </summary>
    IAsyncPolicy WrapRestApiPolicy();
}