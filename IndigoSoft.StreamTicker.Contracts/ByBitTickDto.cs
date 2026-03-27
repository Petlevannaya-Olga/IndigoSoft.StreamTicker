namespace IndigoSoft.StreamTicker.Contracts;

public record ByBitTickDto(string Symbol, double Price, double Volume, long EventTime);
