using Nexo.BuildingBlocks.Domain;

namespace Nexo.Production.Application;

/// <summary>
/// Translates domain events into integration events destined for the transactional outbox.
/// The concrete conversion runs inside <c>ProductionDbContext.SaveChanges</c> (Infrastructure),
/// which serializes each mapped integration event into an <c>OutboxMessage</c> within the same
/// transaction as the state change (Transactional Outbox — see design/02-event-model.md §5.1).
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
