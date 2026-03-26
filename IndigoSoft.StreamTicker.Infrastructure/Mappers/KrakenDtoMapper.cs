using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Infrastructure.Mappers;

public class KrakenDtoMapper: IMapper<KrakenTickDto, Tick>
{
    public Tick Map(KrakenTickDto source)
    {
        throw new NotImplementedException();
    }
}