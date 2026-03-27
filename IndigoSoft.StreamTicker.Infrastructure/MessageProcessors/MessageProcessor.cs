using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;

namespace IndigoSoft.StreamTicker.Infrastructure.MessageProcessors;

public class MessageProcessor<TDto>(IParser<TDto> parser, IMapper<TDto, Tick> mapper)
    : IMessageProcessor<Tick>
{
    public IEnumerable<Tick>? ProcessMessage(string message, CancellationToken ct)
    {
        var dto = parser.Parse(message);
        return dto?.Select(mapper.Map);
    }
}