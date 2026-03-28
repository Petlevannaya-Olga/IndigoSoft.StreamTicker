namespace IndigoSoft.StreamTicker.Contracts;

public sealed class MetricsSnapshot
{
    /// <summary>
    /// Сколько тиков пришло за интервал
    /// </summary>
    public int In { get; init; }

    /// <summary>
    /// Сколько тиков прошло дальше по пайплайну
    /// </summary>
    public int Out { get; init; }

    /// <summary>
    /// Сколько тиков было отфильтровано
    /// </summary>
    public int Deduplicated { get; init; }

    /// <summary>
    /// Сколько батчей записано
    /// </summary>
    public int BatchCount { get; init; }
}