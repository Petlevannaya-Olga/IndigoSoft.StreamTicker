using System.Globalization;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure;
using IndigoSoft.StreamTicker.Infrastructure.Mappers;
using IndigoSoft.StreamTicker.Infrastructure.Parsers;
using IndigoSoft.StreamTicker.Infrastructure.Policies;
using IndigoSoft.StreamTicker.Infrastructure.WebSocketClients;
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
           // services.AddTransient<IWebSocketClient<Tick>, KrakenWebSocketClient>();
            services.AddSingleton<IWebSocketConnector, DefaultWebSocketConnector>();
            services.AddSingleton<IMessageReceiver, DefaultMessageReceiver>();

            services.AddSingleton<IDeduplicator<Tick>, SlidingWindowDeduplicator>();
            services.AddDbContext<TickDbContext>(opt => opt.UseSqlite("Data Source=ticks.db"));
            services.AddScoped<ITickRepository, TickRepository>();
            services.AddHostedService<DataflowPipeline>();

            services.AddTransient<
                IMessageProcessor<Tick>,
                DefaultMessageProcessor<KrakenTickDto, Tick>>();

            services.AddTransient<
                IMessageProcessor<Tick>,
                DefaultMessageProcessor<BinanceTickDto, Tick>>();

            services.AddSingleton<IParser<BinanceTickDto>, BinanceTickParser>();
            services.AddSingleton<IParser<List<KrakenTickDto>>, KrakenTickParser>();

            services.AddSingleton<IMapper<KrakenTickDto, Tick>, KrakenDtoMapper>();
            services.AddSingleton<IMapper<BinanceTickDto, Tick>, BinanceDtoMapper>();
            services.AddSingleton<IWebSocketPolicy, WebSocketPolicy>();
            services.AddSingleton<IMetricsService, MetricsService>();
            services.AddHostedService<MetricsBackgroundService>();
            
            services.AddTransient<IWebSocketClient<Tick>>(sp =>
                new BinanceWebSocketClient(
                    [
                        "btcusdt", "ethusdt", "ethbtc", "ltcbtc", "bnbbtc", "neobtc", "qtumeth", "eoseth", "snteth",
                        "bnteth"
                    ],
                    sp.GetRequiredService<IWebSocketConnector>(),
                    sp.GetRequiredService<IMessageReceiver>(),
                    sp.GetRequiredService<IMessageProcessor<Tick>>(),
                    sp.GetRequiredService<IWebSocketPolicy>(),
                    sp.GetRequiredService<ILogger<WebSocketClientBase<BinanceTickDto, Tick>>>()
                ));
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