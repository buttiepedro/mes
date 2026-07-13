namespace Nexo.BuildingBlocks.Domain;

/// <summary>
/// Base record for domain events. Captures the moment the event occurred.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
