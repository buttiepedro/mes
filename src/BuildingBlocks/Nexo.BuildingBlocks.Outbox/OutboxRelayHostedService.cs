using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.BuildingBlocks.Messaging;

namespace Nexo.BuildingBlocks.Outbox;

/// <summary>
/// Background relay that drains a service's transactional outbox (<c>{schema}.outbox_messages</c>,
/// mapped on <typeparamref name="TContext"/>) to Kafka: every few seconds it reads unprocessed rows,
/// publishes each via <see cref="IOutboxPublisher"/>, and stamps <see cref="OutboxMessage.ProcessedOn"/>.
/// </summary>
/// <remarks>
/// <para>
/// Delivery is <b>at-least-once</b>: a row is marked processed only after a successful publish; a
/// failure leaves it pending (with <see cref="OutboxMessage.Error"/> set) to be retried on the next tick.
/// </para>
/// <para>
/// <b>Multi-tenancy (local scaffold):</b> the scoped <typeparamref name="TContext"/> resolves its
/// connection through the tenant factory, which — with no tenant on the ambient context in a background
/// scope — falls back to the <c>*Default</c> connection (the demo tenant DB locally). The productive
/// relay must iterate the tenants of the Connection Registry and set the tenant context per scope
/// (TODO, see docs/design/01-multi-tenancy-connection.md).
/// </para>
/// </remarks>
public sealed class OutboxRelayHostedService<TContext> : BackgroundService
    where TContext : DbContext
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOutboxPublisher _publisher;
    private readonly ILogger<OutboxRelayHostedService<TContext>> _logger;

    public OutboxRelayHostedService(
        IServiceScopeFactory scopeFactory,
        IOutboxPublisher publisher,
        ILogger<OutboxRelayHostedService<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox relay started for {Context}", typeof(TContext).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never let a bad tick kill the relay; the same rows are retried next tick.
                _logger.LogError(ex, "Outbox relay tick failed for {Context}", typeof(TContext).Name);
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Outbox relay stopping for {Context}", typeof(TContext).Name);
    }

    private async Task DrainOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var pending = await dbContext.Set<OutboxMessage>()
            .Where(message => message.ProcessedOn == null)
            .OrderBy(message => message.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        var published = 0;

        foreach (var message in pending)
        {
            try
            {
                await _publisher.PublishAsync(message, cancellationToken);
                message.ProcessedOn = DateTimeOffset.UtcNow;
                message.Error = null;
                published++;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                _logger.LogWarning(ex, "Failed to publish outbox message {Id} ({Type})", message.Id, message.Type);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (published > 0)
        {
            _logger.LogInformation("Outbox relay published {Count} message(s) for {Context}", published, typeof(TContext).Name);
        }
    }
}
