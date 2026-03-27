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
        services.AddSingleton<IMessageProcessor<Tick>, MessageProcessor<KrakenTickDto, Tick>>();
        services.AddSingleton<MessageProcessor<KrakenTickDto, Tick>>();

        services.AddSingleton<IMessageProcessor<Tick>, MessageProcessor<ByBitTickDto, Tick>>();
        services.AddSingleton<MessageProcessor<ByBitTickDto, Tick>>();
        
        services.AddSingleton<IMessageProcessor<Tick>, MessageProcessor<BinanceTickDto, Tick>>();
        services.AddSingleton<MessageProcessor<BinanceTickDto, Tick>>();
        return services;
    }
}