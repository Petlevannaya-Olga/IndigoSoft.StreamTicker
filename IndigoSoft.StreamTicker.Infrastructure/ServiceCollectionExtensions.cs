using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure.Mappers;
using IndigoSoft.StreamTicker.Infrastructure.MessageProcessors;
using IndigoSoft.StreamTicker.Infrastructure.Options;
using IndigoSoft.StreamTicker.Infrastructure.Parsers;
using IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;
using IndigoSoft.StreamTicker.Infrastructure.WebSocketConnectors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IndigoSoft.StreamTicker.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<DataflowPipelineBackgroundService>();
        services.AddHostedService<MetricsBackgroundService>();
        services.AddSingleton<IMetricsService, MetricsService>();

        return services;
    }

    public static IServiceCollection AddMessageProcessors(this IServiceCollection services)
    {
        services.AddSingleton<IMessageProcessor<Tick>, MessageProcessor<KrakenTickDto>>();
        services.AddSingleton<MessageProcessor<KrakenTickDto>>();

        services.AddSingleton<IMessageProcessor<Tick>, MessageProcessor<ByBitTickDto>>();
        services.AddSingleton<MessageProcessor<ByBitTickDto>>();

        services.AddSingleton<IMessageProcessor<Tick>, MessageProcessor<BinanceTickDto>>();
        services.AddSingleton<MessageProcessor<BinanceTickDto>>();
        return services;
    }

    public static IServiceCollection AddParsers(this IServiceCollection services)
    {
        services.AddSingleton<IParser<BinanceTickDto>, BinanceTickParser>();
        services.AddSingleton<IParser<KrakenTickDto>, KrakenTickParser>();
        services.AddSingleton<IParser<ByBitTickDto>, ByBitTickParser>();
        return services;
    }

    public static IServiceCollection AddMappers(this IServiceCollection services)
    {
        services.AddSingleton<IMapper<KrakenTickDto, Tick>, KrakenDtoMapper>();
        services.AddSingleton<IMapper<BinanceTickDto, Tick>, BinanceDtoMapper>();
        services.AddSingleton<IMapper<ByBitTickDto, Tick>, ByBitDtoMapper>();
        return services;
    }

    public static IServiceCollection AddDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TickDbContext>(opt =>
            opt.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<ITickRepository, TickRepository>();
        return services;
    }

    public static IServiceCollection AddWebSocketConnectors(this IServiceCollection services)
    {
        services.AddSingleton<KrakenWebSocketConnector>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ExchangeOptions>>().Value;
            return new KrakenWebSocketConnector(
                new Uri(options.Kraken.Url),
                options.Kraken.Symbols,
                sp.GetRequiredService<ILogger<KrakenWebSocketConnector>>());
        });

        services.AddSingleton<ByBitWebSocketConnector>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ExchangeOptions>>().Value;
            return new ByBitWebSocketConnector(
                new Uri(options.ByBit.Url),
                options.ByBit.Symbols,
                sp.GetRequiredService<ILogger<ByBitWebSocketConnector>>());
        });

        services.AddSingleton<BinanceWebSocketConnector>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ExchangeOptions>>().Value;
            var uri = new Uri(
                $"wss://stream.binance.com:9443/stream?streams={string.Join("@trade/", options.Binance.Symbols).ToLower()}@trade");
            return new BinanceWebSocketConnector(
                uri,
                sp.GetRequiredService<ILogger<BinanceWebSocketConnector>>());
        });

        return services;
    }
    
    public static IServiceCollection AddWebSocketClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IWebSocketClient<Tick>>(sp => new WebSocketClient<BinanceTickDto, Tick>(
            sp.GetRequiredService<BinanceWebSocketConnector>(),
            sp.GetRequiredService<IMessageReceiver>(),
            sp.GetRequiredService<MessageProcessor<BinanceTickDto>>(),
            sp.GetRequiredService<IWebSocketPolicy>(),
            sp.GetRequiredService<ILogger<WebSocketClient<BinanceTickDto, Tick>>>()
        ));
        
        services.AddTransient<IWebSocketClient<Tick>>(sp => new WebSocketClient<KrakenTickDto, Tick>(
            sp.GetRequiredService<KrakenWebSocketConnector>(),
            sp.GetRequiredService<IMessageReceiver>(),
            sp.GetRequiredService<MessageProcessor<KrakenTickDto>>(),
            sp.GetRequiredService<IWebSocketPolicy>(),
            sp.GetRequiredService<ILogger<WebSocketClient<KrakenTickDto, Tick>>>()
        ));
        
        services.AddTransient<IWebSocketClient<Tick>>(sp => new WebSocketClient<ByBitTickDto, Tick>(
            sp.GetRequiredService<ByBitWebSocketConnector>(),
            sp.GetRequiredService<IMessageReceiver>(),
            sp.GetRequiredService<MessageProcessor<ByBitTickDto>>(),
            sp.GetRequiredService<IWebSocketPolicy>(),
            sp.GetRequiredService<ILogger<WebSocketClient<ByBitTickDto, Tick>>>()
        ));

        // services.AddTransient<IWebSocketClient<Tick>>(sp =>
        //     new KrakenWebSocketClient(
        //         sp.GetRequiredService<KrakenWebSocketConnector>(),
        //         sp.GetRequiredService<IMessageReceiver>(),
        //         sp.GetRequiredService<MessageProcessor<KrakenTickDto>>(),
        //         sp.GetRequiredService<IWebSocketPolicy>(),
        //         sp.GetRequiredService<ILogger<KrakenWebSocketClient>>()
        //     ));
        //
        // services.AddTransient<IWebSocketClient<Tick>>(sp =>
        //     new ByBitWebSocketClient(
        //         sp.GetRequiredService<ByBitWebSocketConnector>(),
        //         sp.GetRequiredService<IMessageReceiver>(),
        //         sp.GetRequiredService<MessageProcessor<ByBitTickDto>>(),
        //         sp.GetRequiredService<IWebSocketPolicy>(),
        //         sp.GetRequiredService<ILogger<ByBitWebSocketClient>>()
        //     ));

        return services;
    }
}