using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Domain;

/// <summary>
/// Base class shared by every master-data aggregate. Carries the natural key (<see cref="Code"/>),
/// the governance mode that makes the hybrid per-catalog mode possible, the lifecycle
/// <see cref="Status"/>, the standard audit block and the soft-delete columns
/// (docs/design/03-data-schema.md §1.3, §1.4 and §2.5).
/// </summary>
/// <remarks>
/// This class is deliberately <b>not</b> part of the EF model (no <c>DbSet</c>, no navigation points
/// at it), so each derived aggregate is mapped as an independent root entity type instead of a
/// table-per-hierarchy.
/// </remarks>
public abstract class MasterRecord : AggregateRoot<Guid>
{
    // EF Core materialization constructor.
    protected MasterRecord() => Code = string.Empty;

    protected MasterRecord(Guid id, string code, MasterGovernance governance, string? externalRef)
    {
        Id = id;
        Code = NormalizeCode(code);
        Governance = governance;
        ExternalRef = Normalize(externalRef);
        Status = MasterStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>Catalog this record belongs to; travels in the integration events.</summary>
    public abstract string Catalog { get; }

    /// <summary>Human-readable label of the record; travels in the integration events.</summary>
    public abstract string DisplayName { get; }

    /// <summary>Natural key of the record within the tenant (unique among non-deleted rows).</summary>
    public string Code { get; protected set; }

    public MasterStatus Status { get; protected set; }

    public MasterGovernance Governance { get; protected set; }

    /// <summary>Identifier of the record in the ERP when the catalog is mirrored or linked.</summary>
    public string? ExternalRef { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; }

    public Guid? CreatedBy { get; protected set; }

    public DateTimeOffset UpdatedAt { get; protected set; }

    public Guid? UpdatedBy { get; protected set; }

    public DateTimeOffset? DeletedAt { get; protected set; }

    public Guid? DeletedBy { get; protected set; }

    public bool IsArchived => Status == MasterStatus.Archived;

    /// <summary>
    /// Logical removal: flips the record to <see cref="MasterStatus.Archived"/> and raises a
    /// <see cref="MasterRecordArchivedDomainEvent"/>. Never a physical delete (R4).
    /// </summary>
    /// <exception cref="InvalidOperationException">When the record is already archived.</exception>
    public void Archive()
    {
        if (Status == MasterStatus.Archived)
        {
            throw new InvalidOperationException($"Record '{Code}' is already archived.");
        }

        Status = MasterStatus.Archived;
        Touch();

        Raise(new MasterRecordArchivedDomainEvent(Catalog, Id, Code));
    }

    /// <summary>Links the record to its ERP counterpart and stamps the synchronization moment.</summary>
    public void LinkToExternal(string externalRef, DateTimeOffset syncedAt)
    {
        var normalized = Normalize(externalRef)
            ?? throw new ArgumentException("External reference cannot be empty.", nameof(externalRef));

        ExternalRef = normalized;
        Governance = MasterGovernance.Linked;
        UpdatedAt = syncedAt;
    }

    protected void RaiseUpserted(MasterRecordChange change)
        => Raise(new MasterRecordUpsertedDomainEvent(Catalog, Id, Code, DisplayName, change));

    protected void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    /// <summary>Trims a mandatory code and rejects null/blank values.</summary>
    /// <exception cref="ArgumentException">When <paramref name="code"/> is null, empty or whitespace.</exception>
    protected static string NormalizeCode(string code, string paramName = "code")
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required and cannot be empty.", paramName);
        }

        return code.Trim();
    }

    /// <summary>Trims a mandatory text and rejects null/blank values.</summary>
    /// <exception cref="ArgumentException">When <paramref name="value"/> is null, empty or whitespace.</exception>
    protected static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"'{paramName}' is required and cannot be empty.", paramName);
        }

        return value.Trim();
    }

    /// <summary>Trims an optional text, collapsing blank values to <c>null</c>.</summary>
    protected static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
