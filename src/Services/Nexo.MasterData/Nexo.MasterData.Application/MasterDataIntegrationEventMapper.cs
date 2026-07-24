using Nexo.BuildingBlocks.Domain;
using Nexo.BuildingBlocks.Messaging;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Application;

/// <summary>
/// Maps Master Data domain events to their public integration-event contracts (with canonical
/// <see cref="EventTypes"/> values). Used by the Infrastructure outbox conversion in SaveChanges.
/// </summary>
public static class MasterDataIntegrationEventMapper
{
    /// <summary>Returns the integration event for a domain event, or <c>null</c> if it is purely internal.</summary>
    public static IIntegrationEvent? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        MasterRecordUpsertedDomainEvent e => new MasterDataRecordUpsertedIntegrationEvent
        {
            Catalog = e.Catalog,
            RecordId = e.RecordId,
            Code = e.Code,
            Name = e.Name,
            Change = e.Change.ToString().ToLowerInvariant(),
            OccurredOn = e.OccurredOn
        },
        MasterRecordArchivedDomainEvent e => new MasterDataRecordArchivedIntegrationEvent
        {
            Catalog = e.Catalog,
            RecordId = e.RecordId,
            Code = e.Code,
            OccurredOn = e.OccurredOn
        },
        _ => null
    };
}
