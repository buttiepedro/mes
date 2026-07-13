namespace Nexo.BuildingBlocks.Messaging;

/// <summary>Publishes integration events to the message bus.</summary>
public interface IEventPublisher
{
    Task PublishAsync(IIntegrationEvent evt, CancellationToken cancellationToken = default);
}
