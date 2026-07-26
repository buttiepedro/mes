using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nexo.BuildingBlocks.Outbox;

/// <summary>Registration helpers for the outbox → Kafka relay.</summary>
public static class OutboxRelayServiceCollectionExtensions
{
    /// <summary>
    /// Registers the singleton Kafka publisher (once) and a hosted <see cref="OutboxRelayHostedService{TContext}"/>
    /// that drains the outbox mapped on <typeparamref name="TContext"/>. Call once per service, passing its DbContext.
    /// </summary>
    public static IServiceCollection AddOutboxRelay<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.TryAddSingleton<IOutboxPublisher, KafkaOutboxPublisher>();
        services.AddHostedService<OutboxRelayHostedService<TContext>>();

        return services;
    }
}
