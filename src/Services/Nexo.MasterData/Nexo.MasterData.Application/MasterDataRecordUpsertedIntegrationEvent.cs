using Nexo.BuildingBlocks.Messaging;

namespace Nexo.MasterData.Application;

/// <summary>
/// Public contract published to the backbone when a master-data record is created or updated.
/// Canonical type: <c>nexo.masterdata.record_upserted</c>. Consumers (WorkModel, Execution,
/// Ingestion) use it to invalidate their catalog caches.
/// </summary>
public sealed record MasterDataRecordUpsertedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.MasterData_RecordUpserted;

    /// <summary>uoms | items | people | customers.</summary>
    public string Catalog { get; init; } = string.Empty;

    public Guid RecordId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    /// <summary>created | updated.</summary>
    public string Change { get; init; } = string.Empty;
}
