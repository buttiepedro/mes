namespace Nexo.MasterData.Domain;

/// <summary>
/// Governance mode of a master-data record (docs/design/03-data-schema.md §2.5,
/// <c>nexo.master_governance_enum</c>). The unit of governance is the catalog, not the tenant.
/// </summary>
public enum MasterGovernance
{
    /// <summary>Exists only in Nexo (standalone mode) — fully editable.</summary>
    Local = 0,

    /// <summary>Imported from the ERP with no Nexo-owned attributes — only non-governed fields are editable.</summary>
    Mirror = 1,

    /// <summary>Lives in both systems with an <c>external_ref</c> established.</summary>
    Linked = 2,

    /// <summary>Unresolved difference on a governed field — blocked, goes to the conflict tray.</summary>
    Divergent = 3
}

/// <summary>
/// Lifecycle of a master-data record. There is no physical delete when events reference the
/// record (R4): archiving is the only way out.
/// </summary>
public enum MasterStatus
{
    Active = 0,
    Archived = 1
}

/// <summary>
/// Role an <see cref="Item"/> plays. Product and input are <b>roles of the same item</b>, not
/// separate catalogs: the finished good of one execution is the input of the next.
/// </summary>
public enum ItemRole
{
    Product = 0,
    Input = 1
}

/// <summary>Traceability granularity required when an <see cref="Item"/> is consumed or produced.</summary>
public enum TrackingMode
{
    None = 0,
    Batch = 1,
    Serial = 2
}

/// <summary>
/// Physical magnitude of a <see cref="Uom"/>. Conversion via <c>factor_to_base</c> is only valid
/// <b>within</b> the same magnitude — going from kg to units requires the item's unit weight.
/// </summary>
public enum UomMagnitude
{
    Mass = 0,
    Length = 1,
    Area = 2,
    Volume = 3,
    Time = 4,
    Count = 5,
    Energy = 6
}

/// <summary>Whether an upsert created a new record or updated an existing one.</summary>
public enum MasterRecordChange
{
    Created = 0,
    Updated = 1
}
