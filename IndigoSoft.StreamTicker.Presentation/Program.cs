using System.Globalization;
using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure;
using IndigoSoft.StreamTicker.Infrastructure.Options;
using IndigoSoft.StreamTicker.Infrastructure.Policies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                .AddMessageConverters()
                .AddBackgroundServices()
                .AddDb(configuration)
                .AddWebSocketConnectors()
                .AddWebSocketClients(configuration);

            services.Configure<ExchangeOptions>(configuration.GetSection("Exchanges"));

            services.AddSingleton<IMessageReceiver, DefaultMessageReceiver>();
            services.AddSingleton<IDeduplicator, SlidingWindowDeduplicator>();
            services.AddSingleton<IWebSocketPolicy, WebSocketPolicy>();
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