using IndigoSoft.StreamTicker.Application;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class DefaultMessageProcessor<TDto, TDomain>(IParser<TDto> parser, IMapper<TDto, TDomain> mapper)
    : IMessageProcessor<TDomain> where TDomain : class
{
    public TDomain? ProcessMessage(string message, CancellationToken ct)
    {
        var dto = parser.Parse(message);
        return dto is null ? null : mapper.Map(dto);
    }
}