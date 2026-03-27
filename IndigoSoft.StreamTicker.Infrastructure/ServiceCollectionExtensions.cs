using IndigoSoft.StreamTicker.Application;
using IndigoSoft.StreamTicker.Contracts;
using IndigoSoft.StreamTicker.Domain;
using IndigoSoft.StreamTicker.Infrastructure.MessageProcessors;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<IMessageProcessor<Tick>, SingleMessageProcessor<BinanceTickDto, Tick>>();
        services.AddSingleton<IMessageProcessor<Tick>, SingleMessageProcessor<ByBitTickDto, Tick>>();
        services.AddSingleton<IMessageProcessor<Tick>, CollectionMessageProcessor<KrakenTickDto, Tick>>();
        return services;
    }
}