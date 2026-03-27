using IndigoSoft.StreamTicker.Application;

namespace IndigoSoft.StreamTicker.Infrastructure.MessageProcessors;

public class MessageProcessor<TDto, TDomain>(IParser<TDto> parser, IMapper<TDto, TDomain> mapper)
    : IMessageProcessor<TDomain>
{
    public IEnumerable<TDomain>? ProcessMessage(string message, CancellationToken ct)
    {
        var dto = parser.Parse(message);
        return dto?.Select(mapper.Map);
    }
}