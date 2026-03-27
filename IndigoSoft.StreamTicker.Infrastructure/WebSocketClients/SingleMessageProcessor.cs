using IndigoSoft.StreamTicker.Application;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class SingleMessageProcessor<TDto, TDomain>(IParser<TDto> parser, IMapper<TDto, TDomain> mapper)
    : IMessageProcessor<TDomain> where TDomain : class
{
    public IEnumerable<TDomain>? ProcessMessage(string message, CancellationToken ct)
    {
        var dto = parser.Parse(message);
        return dto is null ? null : new[] { mapper.Map(dto) };
    }
}