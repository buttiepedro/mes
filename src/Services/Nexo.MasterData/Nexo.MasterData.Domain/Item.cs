using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Domain;

/// <summary>
/// Item (<c>master.items</c>). Aggregate root. <b>Product and input are roles of the same item</b>,
/// not two catalogs: the finished good of one execution is the input of the next, and splitting them
/// would break multi-level genealogy (docs/design/03-data-schema.md §2.5.2).
/// </summary>
public sealed class Item : MasterRecord
{
    private IReadOnlyCollection<ItemRole> _roles = Array.Empty<ItemRole>();

    // EF Core materialization constructor.
    private Item() => Name = string.Empty;

    private Item(
        Guid id,
        string code,
        string name,
        Guid baseUomId,
        IEnumerable<ItemRole> roles,
        TrackingMode tracking,
        string? category,
        string? family,
        decimal? idealCycleTime,
        Guid? defaultProcessId,
        string? qualitySpecs,
        MasterGovernance governance,
        string? externalRef)
        : base(id, code, governance, externalRef)
    {
        Name = NormalizeRequired(name, nameof(name));
        BaseUomId = EnsureBaseUom(baseUomId);
        _roles = EnsureRoles(roles);
        Tracking = tracking;
        Category = Normalize(category);
        Family = Normalize(family);
        IdealCycleTime = EnsureIdealCycleTime(idealCycleTime);
        DefaultProcessId = defaultProcessId;
        QualitySpecs = Normalize(qualitySpecs);
    }

    public override string Catalog => MasterCatalog.Items;

    public override string DisplayName => Name;

    public string Name { get; private set; }

    /// <summary>Base unit of the item — the absolute floor is code + name + base unit.</summary>
    public Guid BaseUomId { get; private set; }

    /// <summary>Roles played by the item; always at least one.</summary>
    public IReadOnlyCollection<ItemRole> Roles => _roles;

    /// <summary>material | component | tool | service | external_labor.</summary>
    public string? Category { get; private set; }

    public string? Family { get; private set; }

    public TrackingMode Tracking { get; private set; }

    /// <summary>Ideal cycle time for the product role in a repetitive profile (overridable per work center, MOD-06).</summary>
    public decimal? IdealCycleTime { get; private set; }

    /// <summary>
    /// Default process (<c>work.processes</c>). Logical reference, no physical foreign key: the
    /// <c>work</c> schema belongs to Nexo.WorkModel and is not part of this service's model.
    /// </summary>
    public Guid? DefaultProcessId { get; private set; }

    /// <summary>Quality specification payload (jsonb).</summary>
    public string? QualitySpecs { get; private set; }

    /// <summary>Moment of the last synchronization with the ERP, when the catalog is mirrored or linked.</summary>
    public DateTimeOffset? LastSyncedAt { get; private set; }

    public bool HasRole(ItemRole role) => _roles.Contains(role);

    /// <summary>
    /// Creates an item and raises the upserted domain event.
    /// </summary>
    /// <exception cref="ArgumentException">When the code or name are empty, or when no role is supplied.</exception>
    public static Item Create(
        string code,
        string name,
        Guid baseUomId,
        IEnumerable<ItemRole> roles,
        TrackingMode tracking = TrackingMode.None,
        string? category = null,
        string? family = null,
        decimal? idealCycleTime = null,
        Guid? defaultProcessId = null,
        string? qualitySpecs = null,
        MasterGovernance governance = MasterGovernance.Local,
        string? externalRef = null)
    {
        var item = new Item(
            UuidV7.NewGuid(),
            code,
            name,
            baseUomId,
            roles,
            tracking,
            category,
            family,
            idealCycleTime,
            defaultProcessId,
            qualitySpecs,
            governance,
            externalRef);

        item.RaiseUpserted(MasterRecordChange.Created);

        return item;
    }

    /// <summary>Updates the editable attributes of the item and raises the upserted domain event.</summary>
    /// <exception cref="InvalidOperationException">When the item is archived.</exception>
    /// <exception cref="ArgumentException">When the name is empty or no role is supplied.</exception>
    public void Update(
        string name,
        IEnumerable<ItemRole> roles,
        TrackingMode tracking,
        string? category = null,
        string? family = null,
        decimal? idealCycleTime = null,
        Guid? defaultProcessId = null,
        string? qualitySpecs = null)
    {
        if (IsArchived)
        {
            throw new InvalidOperationException($"Item '{Code}' is archived and cannot be updated.");
        }

        Name = NormalizeRequired(name, nameof(name));
        _roles = EnsureRoles(roles);
        Tracking = tracking;
        Category = Normalize(category);
        Family = Normalize(family);
        IdealCycleTime = EnsureIdealCycleTime(idealCycleTime);
        DefaultProcessId = defaultProcessId;
        QualitySpecs = Normalize(qualitySpecs);
        Touch();

        RaiseUpserted(MasterRecordChange.Updated);
    }

    /// <summary>Re-points the item to another base unit (only while no history has been valued with it).</summary>
    public void ChangeBaseUom(Guid baseUomId)
    {
        BaseUomId = EnsureBaseUom(baseUomId);
        Touch();

        RaiseUpserted(MasterRecordChange.Updated);
    }

    /// <summary>Stamps the moment of the last successful synchronization with the ERP.</summary>
    public void MarkSynced(DateTimeOffset syncedAt) => LastSyncedAt = syncedAt;

    private static IReadOnlyCollection<ItemRole> EnsureRoles(IEnumerable<ItemRole> roles)
    {
        var distinct = (roles ?? Enumerable.Empty<ItemRole>()).Distinct().OrderBy(role => role).ToArray();

        if (distinct.Length == 0)
        {
            throw new ArgumentException("An item must declare at least one role (product and/or input).", nameof(roles));
        }

        return distinct;
    }

    private static Guid EnsureBaseUom(Guid baseUomId)
    {
        if (baseUomId == Guid.Empty)
        {
            throw new ArgumentException("An item must reference a base unit of measure.", nameof(baseUomId));
        }

        return baseUomId;
    }

    private static decimal? EnsureIdealCycleTime(decimal? idealCycleTime)
    {
        if (idealCycleTime is <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idealCycleTime),
                idealCycleTime,
                "The ideal cycle time must be greater than zero when supplied.");
        }

        return idealCycleTime;
    }
}
