namespace Nexo.BuildingBlocks.Domain;

/// <summary>
/// Marker for something meaningful that happened inside an aggregate.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
