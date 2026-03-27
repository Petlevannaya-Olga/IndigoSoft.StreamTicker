using IndigoSoft.StreamTicker.Application;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class CollectionMessageProcessor<TDto, TDomain>(IParser<IEnumerable<TDto>> parser, IMapper<TDto, TDomain> mapper)
    : IMessageProcessor<TDomain> where TDomain : class
{
    public IEnumerable<TDomain>? ProcessMessage(string message, CancellationToken ct)
    {
        var dto = parser.Parse(message);
        return dto?.Select(mapper.Map);
    }
}