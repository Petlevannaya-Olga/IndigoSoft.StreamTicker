using System.Globalization;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Infrastructure.Mappers;

public class BinanceDtoMapper : IMapper<BinanceTickDto, Tick>
{
    public Tick Map(BinanceTickDto source)
    {
        return new Tick
        {
            Exchange = nameof(AvailableExchanges.Binance),
            Symbol = source.Data.Symbol,
            Price = double.Parse(source.Data.Price, CultureInfo.InvariantCulture),
            Volume = double.Parse(source.Data.Volume, CultureInfo.InvariantCulture),
            EventTime = source.Data.EventTime
        };
    }
}