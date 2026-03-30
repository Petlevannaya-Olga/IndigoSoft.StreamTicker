using System.Threading.Tasks.Dataflow;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure.BackgroundServices;
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
        services.AddKeyedSingleton<KrakenWebSocketConnector>("kraken",
            (sp, key) =>
            {
                var options = sp.GetRequiredService<IOptions<ExchangeOptions>>().Value;

                return new KrakenWebSocketConnector(
                    new Uri(options.Kraken.Url),
                    options.Kraken.Symbols,
                    sp.GetRequiredService<ILogger<KrakenWebSocketConnector>>());
            });

        services.AddKeyedSingleton<ByBitWebSocketConnector>("bybit",
            (sp, key) =>
            {
                var options = sp.GetRequiredService<IOptions<ExchangeOptions>>().Value;

                return new ByBitWebSocketConnector(
                    new Uri(options.ByBit.Url),
                    options.ByBit.Symbols,
                    sp.GetRequiredService<ILogger<ByBitWebSocketConnector>>());
            });

        services.AddKeyedSingleton<BinanceWebSocketConnector>("binance-1",
            (sp, key) =>
            {
               // var symbols = new[] { "btcusdt", "ethbtc","ltcbtc","bnbbtc","neobtc","qtumeth","eoseth","snteth","bnteth","bccbtc","gasbtc","bnbeth","btcusdt","ethusdt","hsrbtc","oaxeth" };
               var options = sp.GetRequiredService<IOptions<ExchangeOptions>>().Value;

                var uri = new Uri(
                    $"wss://stream.binance.com:9443/stream?streams={string.Join("@trade/", options.Binance.Symbols.Skip(30).Take(30)).ToLower()}@trade");

                return new BinanceWebSocketConnector(
                    uri,
                    sp.GetRequiredService<ILogger<BinanceWebSocketConnector>>());
            });

        services.AddKeyedSingleton<BinanceWebSocketConnector>("binance-2",
            (sp, key) =>
            {
                //var symbols = new[] { "dnteth","mcoeth","icneth","mcobtc","wtcbtc","wtceth","lrcbtc","lrceth","qtumbtc","yoyobtc","omgbtc","omgeth","zrxbtc","zrxeth","stratbtc" };
                var options = sp.GetRequiredService<IOptions<ExchangeOptions>>().Value;
                
                var uri = new Uri(
                    $"wss://stream.binance.com:9443/stream?streams={string.Join("@trade/", options.Binance.Symbols.Take(30)).ToLower()}@trade");

                return new BinanceWebSocketConnector(
                    uri,
                    sp.GetRequiredService<ILogger<BinanceWebSocketConnector>>());
            });
        
        // services.AddKeyedSingleton<BinanceWebSocketConnector>("binance-3",
        //     (sp, key) =>
        //     {
        //         var symbols = new[] { "strateth","snglsbtc","snglseth","bqxbtc","bqxeth","kncbtc","knceth","funbtc","funeth","snmbtc","snmeth","neoeth","iotabtc","iotaeth","linkbtc" };
        //
        //         var uri = new Uri(
        //             $"wss://stream.binance.com:9443/stream?streams={string.Join("@trade/", symbols).ToLower()}@trade");
        //
        //         return new BinanceWebSocketConnector(
        //             uri,
        //             sp.GetRequiredService<ILogger<BinanceWebSocketConnector>>());
        //     });
        
        services.AddKeyedSingleton<BinanceWebSocketConnector>("binance-4",
            (sp, key) =>
            {
                var uri = new Uri(
                    $"ws://localhost:5000/ws"); // для использования фейковой генерации тиков

                return new BinanceWebSocketConnector(
                    uri,
                    sp.GetRequiredService<ILogger<BinanceWebSocketConnector>>());
            });

        return services;
    }

    public static IServiceCollection AddWebSocketClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IWebSocketClient<ITargetBlock<Tick>>>(sp =>
            CreateClient<BinanceWebSocketConnector, BinanceMessageConverter>(sp, "binance-1"));

        services.AddTransient<IWebSocketClient<ITargetBlock<Tick>>>(sp =>
            CreateClient<BinanceWebSocketConnector, BinanceMessageConverter>(sp, "binance-2"));
        
        // services.AddTransient<IWebSocketClient<ITargetBlock<Tick>>>(sp =>
        //     CreateClient<BinanceWebSocketConnector, BinanceMessageConverter>(sp, "binance-3"));
        
        services.AddTransient<IWebSocketClient<ITargetBlock<Tick>>>(sp =>
            CreateClient<BinanceWebSocketConnector, BinanceMessageConverter>(sp, "binance-4"));

        services.AddTransient<IWebSocketClient<ITargetBlock<Tick>>>(sp =>
            CreateClient<KrakenWebSocketConnector, KrakenMessageConverter>(sp, "kraken"));

        services.AddTransient<IWebSocketClient<ITargetBlock<Tick>>>(sp =>
            CreateClient<ByBitWebSocketConnector, ByBitMessageConverter>(sp, "bybit"));

        return services;
    }

    private static DataflowWebSocketClient CreateClient<TConnector, TConverter>(
        IServiceProvider sp,
        object key)
        where TConnector : IWebSocketConnector
        where TConverter : IMessageConverter
    {
        return new DataflowWebSocketClient(
            sp.GetRequiredKeyedService<TConnector>(key),
            sp.GetRequiredService<IMessageReceiver>(),
            sp.GetRequiredService<TConverter>(),
            sp.GetRequiredService<IWebSocketPolicy>(),
            sp.GetRequiredService<ILogger<DataflowWebSocketClient>>()
        );
    }
}