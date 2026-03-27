using System.Globalization;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Infrastructure.Mappers;

public class BinanceDtoMapper : IMapper<BinanceTickDto, Tick>
{
    public Tick Map(BinanceTickDto source)
    {
        return new Tick(
            Guid.NewGuid(),
            nameof(AvailableExchanges.Binance),
            source.Data.Symbol,
            double.Parse(source.Data.Price, CultureInfo.InvariantCulture),
            double.Parse(source.Data.Volume, CultureInfo.InvariantCulture),
            source.Data.EventTime);
    }
}