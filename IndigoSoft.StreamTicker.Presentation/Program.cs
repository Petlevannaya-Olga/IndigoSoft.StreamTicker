using System.Globalization;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure;
using IndigoSoft.StreamTicker.Infrastructure.Mappers;
using IndigoSoft.StreamTicker.Infrastructure.MessageProcessors;
using IndigoSoft.StreamTicker.Infrastructure.Parsers;
using IndigoSoft.StreamTicker.Infrastructure.Policies;
using IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;
using IndigoSoft.StreamTicker.Infrastructure.WebSocketConnectors;
using IndigoSoft.StreamTicker.Presentation.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Exceptions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Application starting...");

    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    using var host = Host.CreateDefaultBuilder(args)
        .UseSerilog((context, services, config) =>
        {
            config
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Enrich.WithProperty("ServiceName", "indigoSoft.StreamTicker");
        })
        .ConfigureServices(services =>
        {
            services
                .AddMessageProcessors()
                .AddBackgroundServices();
                
                
            services.AddSingleton<IWebSocketConnector, DefaultWebSocketConnector>();
            services.AddSingleton<IMessageReceiver, DefaultMessageReceiver>();
            
            services.AddSingleton<IDeduplicator<Tick>, SlidingWindowDeduplicator>();
            services.AddDbContext<TickDbContext>(opt => opt.UseSqlite("Data Source=ticks.db"));
            services.AddScoped<ITickRepository, TickRepository>();
            
            
            services.AddSingleton<IParser<BinanceTickDto>, BinanceTickParser>();
            services.AddSingleton<IParser<KrakenTickDto>, KrakenTickParser>();
            services.AddSingleton<IParser<ByBitTickDto>, ByBitTickParser>();
            
            services.AddSingleton<IMapper<KrakenTickDto, Tick>, KrakenDtoMapper>();
            services.AddSingleton<IMapper<BinanceTickDto, Tick>, BinanceDtoMapper>();
            services.AddSingleton<IMapper<ByBitTickDto, Tick>, ByBitDtoMapper>();
            services.AddSingleton<IWebSocketPolicy, WebSocketPolicy>();
            
            services.AddSingleton<KrakenWebSocketConnector>(sp =>
                new KrakenWebSocketConnector(["XBT/USD", "ETH/USD", "SOL/USD", "BTC/USDT", "ETH/USDT"],
                    sp.GetRequiredService<ILogger<KrakenWebSocketConnector>>()));
            
            services.AddSingleton<ByBitWebSocketConnector>(sp =>
                new ByBitWebSocketConnector([
                        "BTCUSDT", "ETHUSDT", "SOLUSDT", "XRPUSDT", "ADAUSDT",
                        "DOGEUSDT", "AVAXUSDT", "DOTUSDT", "MATICUSDT", "LTCUSDT"
                    ],
                    sp.GetRequiredService<ILogger<ByBitWebSocketConnector>>()));
            
            services.AddTransient<IWebSocketClient<Tick>>(sp =>
                new BinanceWebSocketClient(
                    [
                        "btcusdt", "ethusdt", "ethbtc", "ltcbtc", "bnbbtc", "neobtc", "qtumeth", "eoseth", "snteth",
                        "bnteth"
                    ],
                    sp.GetRequiredService<IWebSocketConnector>(),
                    sp.GetRequiredService<IMessageReceiver>(),
                    sp.GetRequiredService<MessageProcessor<BinanceTickDto, Tick>>(),
                    sp.GetRequiredService<IWebSocketPolicy>(),
                    sp.GetRequiredService<ILogger<BinanceWebSocketClient>>()
                ));
            
            services.AddTransient<IWebSocketClient<Tick>>(sp =>
                new KrakenWebSocketClient(
                    sp.GetRequiredService<KrakenWebSocketConnector>(),
                    sp.GetRequiredService<IMessageReceiver>(),
                    sp.GetRequiredService<MessageProcessor<KrakenTickDto, Tick>>(),
                    sp.GetRequiredService<IWebSocketPolicy>(),
                    sp.GetRequiredService<ILogger<KrakenWebSocketClient>>()
                ));
            
            services.AddTransient<IWebSocketClient<Tick>>(sp =>
                new ByBitWebSocketClient(
                    sp.GetRequiredService<ByBitWebSocketConnector>(),
                    sp.GetRequiredService<IMessageReceiver>(),
                    sp.GetRequiredService<MessageProcessor<ByBitTickDto, Tick>>(),
                    sp.GetRequiredService<IWebSocketPolicy>(),
                    sp.GetRequiredService<ILogger<ByBitWebSocketClient>>()
                ));
            
            // services
            //     .AddWebSockets()
            //     .AddProcessors()
            //     .AddParsersAndMappers()
            //     .AddData()
            //     .AddBackground();
            //
            // services.Configure<KrakenOptions>(configuration.GetSection("Kraken"));
            //
            // services.AddTransient<BinanceWebSocketClient>();
            // services.AddTransient<KrakenWebSocketClient>();
            // services.AddTransient<ByBitWebSocketClient>();
            //
            // services.AddTransient<IWebSocketClient<Tick>, BinanceWebSocketClient>();
            // services.AddTransient<IWebSocketClient<Tick>, KrakenWebSocketClient>();
            // services.AddTransient<IWebSocketClient<Tick>, ByBitWebSocketClient>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception e)
{
    Log.Fatal(e, "Unable to start application");
}
finally
{
    Log.CloseAndFlush();
}