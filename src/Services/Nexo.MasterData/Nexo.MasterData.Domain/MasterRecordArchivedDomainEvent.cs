using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Domain;

/// <summary>
/// Raised when a master-data record is archived (logical removal — never a physical delete
/// while events reference it).
/// Translated to the canonical integration event <c>nexo.masterdata.record_archived</c> by the Application layer.
/// </summary>
public sealed record MasterRecordArchivedDomainEvent(
    string Catalog,
    Guid RecordId,
    string Code) : DomainEvent;
