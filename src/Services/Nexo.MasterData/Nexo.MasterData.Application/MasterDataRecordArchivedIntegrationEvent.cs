using Nexo.BuildingBlocks.Messaging;

namespace Nexo.MasterData.Application;

/// <summary>
/// Public contract published to the backbone when a master-data record is archived.
/// Canonical type: <c>nexo.masterdata.record_archived</c>. WorkModel warns on publication,
/// Execution and Audit react to it.
/// </summary>
public sealed record MasterDataRecordArchivedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.MasterData_RecordArchived;

    /// <summary>uoms | items | people | customers.</summary>
    public string Catalog { get; init; } = string.Empty;

    public Guid RecordId { get; init; }

    public string Code { get; init; } = string.Empty;
}
