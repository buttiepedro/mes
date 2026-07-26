using Nexo.BuildingBlocks.Messaging;

namespace Nexo.BuildingBlocks.Outbox;

/// <summary>
/// Publishes a persisted <see cref="OutboxMessage"/> to the message bus (Kafka). Implementations must
/// be idempotent-friendly: the relay marks a message processed only after a successful publish, so a
/// crash between publish and commit can re-deliver (at-least-once) — downstream dedup handles it.
/// </summary>
public interface IOutboxPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
