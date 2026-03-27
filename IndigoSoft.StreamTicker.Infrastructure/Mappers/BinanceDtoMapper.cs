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
            nameof(AvailableExchanges.Binance),
            source.Data.Symbol,
            source.Data.PriceValue,
            source.Data.VolumeValue,
            source.Data.EventTime
        );
    }
}