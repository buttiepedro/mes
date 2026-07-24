using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Domain;

/// <summary>
/// Operational person (<c>master.people</c>). Aggregate root.
/// </summary>
/// <remarks>
/// Three-way split (docs/design/03-data-schema.md §2.5.3): <b>identity and credentials</b> live in the
/// Control Plane (TEN-07); the <b>fast-capture profile</b> (PIN/badge/NFC) lives in <c>config.operators</c>;
/// the <b>operational dimension</b> (employee number, preferred role, scope, availability) lives here.
/// A person may exist <b>without</b> a user account. There is no hourly rate: cost is deferred to V1 (§2.5.5).
/// </remarks>
public sealed class Person : MasterRecord
{
    // EF Core materialization constructor.
    private Person() => FullName = string.Empty;

    private Person(
        Guid id,
        string code,
        string fullName,
        Guid? defaultRoleId,
        Guid? siteId,
        Guid? lineId,
        Guid? userId,
        string? calendar,
        MasterGovernance governance,
        string? externalRef)
        : base(id, code, governance, externalRef)
    {
        FullName = NormalizeRequired(fullName, nameof(fullName));
        DefaultRoleId = defaultRoleId;
        SiteId = siteId;
        LineId = lineId;
        UserId = userId;
        Calendar = Normalize(calendar);
    }

    public override string Catalog => MasterCatalog.People;

    public override string DisplayName => FullName;

    public string FullName { get; private set; }

    /// <summary>Preferred operational role (<c>config.roles</c>). Logical reference, no physical foreign key.</summary>
    public Guid? DefaultRoleId { get; private set; }

    /// <summary>Default site scope (<c>config.sites</c>). Logical reference, no physical foreign key.</summary>
    public Guid? SiteId { get; private set; }

    /// <summary>Default line scope (<c>config.lines</c>). Logical reference, no physical foreign key.</summary>
    public Guid? LineId { get; private set; }

    /// <summary>Global identity of the person, when they have one. Logical reference to the Control Plane (§1.9).</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Own availability calendar (jsonb) — relevant for the project flavour.</summary>
    public string? Calendar { get; private set; }

    /// <summary>Creates an operational person and raises the upserted domain event.</summary>
    /// <exception cref="ArgumentException">When the code or the full name are empty.</exception>
    public static Person Create(
        string code,
        string fullName,
        Guid? defaultRoleId = null,
        Guid? siteId = null,
        Guid? lineId = null,
        Guid? userId = null,
        string? calendar = null,
        MasterGovernance governance = MasterGovernance.Local,
        string? externalRef = null)
    {
        var person = new Person(
            UuidV7.NewGuid(),
            code,
            fullName,
            defaultRoleId,
            siteId,
            lineId,
            userId,
            calendar,
            governance,
            externalRef);

        person.RaiseUpserted(MasterRecordChange.Created);

        return person;
    }

    /// <summary>Updates the editable attributes of the person and raises the upserted domain event.</summary>
    /// <exception cref="InvalidOperationException">When the person is archived.</exception>
    public void Update(
        string fullName,
        Guid? defaultRoleId = null,
        Guid? siteId = null,
        Guid? lineId = null,
        string? calendar = null)
    {
        if (IsArchived)
        {
            throw new InvalidOperationException($"Person '{Code}' is archived and cannot be updated.");
        }

        FullName = NormalizeRequired(fullName, nameof(fullName));
        DefaultRoleId = defaultRoleId;
        SiteId = siteId;
        LineId = lineId;
        Calendar = Normalize(calendar);
        Touch();

        RaiseUpserted(MasterRecordChange.Updated);
    }

    /// <summary>Links the person to a global identity (a badge-only operator never gets one).</summary>
    public void LinkUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("The user identifier cannot be empty.", nameof(userId));
        }

        UserId = userId;
        Touch();
    }
}
