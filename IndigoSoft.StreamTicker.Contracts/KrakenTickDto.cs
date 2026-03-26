namespace IndigoSoft.StreamTicker.Contracts;

public record KrakenTickDto(string Symbol, double Price, double Volume, long EventTime);