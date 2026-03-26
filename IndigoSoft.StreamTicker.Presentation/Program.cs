using System.Globalization;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure;
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
            services.AddSingleton<BinancePolicies>();
            services.AddSingleton<KrakenPolicies>();

            services.AddSingleton<IWebSocketClient>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<BinanceWebSocketClient>>();
                var policies = sp.GetRequiredService<BinancePolicies>();
                var symbols = new[] { "btcusdt","ethusdt","ethbtc","ltcbtc","bnbbtc","neobtc","qtumeth","eoseth","snteth","bnteth", };
                return new BinanceWebSocketClient(symbols, policies, logger);
            });

            services.AddSingleton<IWebSocketClient>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<KrakenWebSocketClient>>();
                var policies = sp.GetRequiredService<KrakenPolicies>();
                var symbols = new[] { "XBT/USD", "ETH/USD", "SOL/USD", "BTC/USDT", "ETH/USDT" };
                return new KrakenWebSocketClient(symbols, policies, logger);
            });

            services.AddSingleton<IDeduplicator<Tick>, SlidingWindowDeduplicator<Tick>>();
            services.AddDbContext<TickDbContext>(opt => opt.UseSqlite("Data Source=ticks.db"));
            services.AddScoped<ITickRepository, TickRepository>();
            services.AddHostedService<TickProcessor>();
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