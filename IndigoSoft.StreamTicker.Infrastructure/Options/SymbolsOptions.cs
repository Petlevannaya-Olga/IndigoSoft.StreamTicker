namespace IndigoSoft.StreamTicker.Infrastructure.Options;

public class SymbolsOptions
{
    public string[] Kraken { get; set; } = [];
    public string[] Binance { get; set; } = [];
    public string[] ByBit { get; set; } = [];
}

public class ExchangeOptions
{
    public KrakenOptions Kraken { get; set; } = new ();
    public BinanceOptions Binance { get; set; } = new ();
    public ByBitOptions ByBit { get; set; } = new ();
}

public class KrakenOptions
{
    public string[] Symbols { get; set; } = [];
    public string Url { get; set; } = string.Empty;
}

public class BinanceOptions
{
    public string[] Symbols { get; set; } = [];
    public string Url { get; set; } = string.Empty;
}

public class ByBitOptions
{
    public string[] Symbols { get; set; } = [];
    public string Url { get; set; } = string.Empty;
}