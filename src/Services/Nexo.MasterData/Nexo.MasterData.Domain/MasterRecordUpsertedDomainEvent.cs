using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Domain;

/// <summary>
/// Raised when a master-data record is created or updated.
/// Translated to the canonical integration event <c>nexo.masterdata.record_upserted</c> by the Application layer.
/// </summary>
public sealed record MasterRecordUpsertedDomainEvent(
    string Catalog,
    Guid RecordId,
    string Code,
    string Name,
    MasterRecordChange Change) : DomainEvent;
