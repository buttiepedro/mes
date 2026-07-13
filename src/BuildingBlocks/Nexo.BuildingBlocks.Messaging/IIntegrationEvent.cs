namespace Nexo.BuildingBlocks.Messaging;

/// <summary>A cross-service event carried over the message bus / outbox.</summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }

    string Type { get; }

    DateTimeOffset OccurredOn { get; }
}
