using IndigoSoft.StreamTicker.Application;

namespace IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;

public class DefaultMessageProcessor<TDto, TDomain>(IParser<TDto> parser, IMapper<TDto, TDomain> mapper)
    : IMessageProcessor<TDomain> where TDomain : class
{
    public Task<TDomain?> ProcessMessageAsync(string message, CancellationToken ct)
    {
        var dto = parser.Parse(message);
        if (dto is null)
            return Task.FromResult<TDomain?>(null);

        var item = mapper.Map(dto);
        return Task.FromResult<TDomain?>(item);
    }
}