using Nexo.BuildingBlocks.Domain;

namespace Nexo.BuildingBlocks.Messaging;

/// <summary>
/// Base record for integration events. Derived records supply the canonical
/// <see cref="Type"/> from <see cref="EventTypes"/>.
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = UuidV7.NewGuid();

    public abstract string Type { get; }

    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
