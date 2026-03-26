namespace IndigoSoft.StreamTicker.Domain;

public record Tick(Guid Id, string Exchange, string Symbol, double Price, double Volume, long EventTime);