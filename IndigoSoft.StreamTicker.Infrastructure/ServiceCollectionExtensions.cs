using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Infrastructure.MessageConverters;
using IndigoSoft.StreamTicker.Infrastructure.Options;
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
        services.AddHostedService<PipelineBackgroundService>();
        services.AddHostedService<MetricsBackgroundService>();
        services.AddSingleton<IMetricsService, MetricsService>();

        return services;
    }

    public static IServiceCollection AddMessageConverters(this IServiceCollection services)
    {
        services.AddSingleton<IMessageConverter, BinanceMessageConverter>();
        services.AddSingleton<BinanceMessageConverter>();
        
        services.AddSingleton<IMessageConverter, KrakenMessageConverter>();
        services.AddSingleton<KrakenMessageConverter>();
        
        services.AddSingleton<IMessageConverter, ByBitMessageConverter>();
        services.AddSingleton<ByBitMessageConverter>();
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
        services.AddTransient<DataflowWebSocketClient>(CreateClient<BinanceWebSocketConnector, BinanceMessageConverter>);
        services.AddTransient<DataflowWebSocketClient>(CreateClient<KrakenWebSocketConnector, KrakenMessageConverter>);
        services.AddTransient<DataflowWebSocketClient>(CreateClient<ByBitWebSocketConnector, ByBitMessageConverter>);

        return services;
    }
    
    private static DataflowWebSocketClient CreateClient<TConnector, TConverter>(IServiceProvider sp)
        where TConnector : IWebSocketConnector
        where TConverter : IMessageConverter
    {
        return new DataflowWebSocketClient(
            sp.GetRequiredService<TConnector>(),
            sp.GetRequiredService<IMessageReceiver>(),
            sp.GetRequiredService<TConverter>(),
            sp.GetRequiredService<IWebSocketPolicy>(),
            sp.GetRequiredService<ILogger<DataflowWebSocketClient>>()
        );
    }
}