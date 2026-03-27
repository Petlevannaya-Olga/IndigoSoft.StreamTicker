using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Infrastructure.Mappers;

public class ByBitDtoMapper: IMapper<ByBitTickDto, Tick>
{
    public Tick Map(ByBitTickDto source)
    {
        return new Tick(
            nameof(AvailableExchanges.ByBit),
            source.Symbol,
            source.Price,
            source.Volume,
            source.EventTime
        );
    }
}