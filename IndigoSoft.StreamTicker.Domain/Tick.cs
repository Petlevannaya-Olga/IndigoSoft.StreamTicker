namespace IndigoSoft.StreamTicker.Domain;

public class Tick
{
    public Guid Id { get; set; }
    public string Exchange { get; set; }
    public string Symbol { get; set; }
    public double Price { get; set; }
    public double Volume { get; set; }
    public long EventTime { get; set; } // TODO Datetime
    
    // EF Core
    public Tick()
    {
    }

    public Tick(string exchange, string symbol, double price, double volume, long eventTime)
    {
        Id = Guid.NewGuid();
        Exchange = exchange;
        Symbol = symbol;
        Price = price;
        Volume = volume;
        EventTime = eventTime;
    }
}