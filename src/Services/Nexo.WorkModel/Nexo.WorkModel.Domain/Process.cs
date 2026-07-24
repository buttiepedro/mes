using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Domain;

/// <summary>
/// A process (<c>work.processes</c>): the <b>template</b>, reusable and versioned. Aggregate root.
/// Its identity (<see cref="Code"/>) is stable across every version.
/// </summary>
/// <remarks>
/// It knows nothing about executions, has no operational state and no real quantities: that is Layer
/// 3 (docs/design/03-data-schema.md §2.6). <see cref="Profile"/> is the only attribute that tells
/// "making windows" apart from "building a site".
/// <para>
/// <see cref="OutputItemId"/> / <see cref="OutputUomId"/> point at <c>master.items</c> /
/// <c>master.uom</c>, and the scope ids at <c>config.*</c>: all of them are <b>logical references
/// without a physical foreign key</b> (§1.9), so the migrations of the bounded contexts stay
/// independent even though the tenant's services share one physical database.
/// </para>
/// <para><b>No cost.</b> No standard cost, no rates anywhere in this model (MOD-17).</para>
/// </remarks>
public sealed class Process : AggregateRoot<Guid>
{
    private string[] _tags = Array.Empty<string>();

    // EF Core materialization constructor.
    private Process()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private Process(
        Guid id,
        string code,
        string name,
        ProcessProfile profile,
        Guid? outputItemId,
        Guid? outputUomId,
        Guid? siteId,
        Guid? areaId,
        Guid? lineId,
        EvidencePolicy evidencePolicy,
        SkipPolicy skipPolicy,
        IEnumerable<string>? tags,
        string? externalRef)
        : base(id)
    {
        Code = NormalizeRequired(code, "code");
        Name = NormalizeRequired(name, "name");
        Profile = profile;
        OutputItemId = outputItemId;
        OutputUomId = outputUomId;
        SiteId = siteId;
        AreaId = areaId;
        LineId = lineId;
        EvidencePolicy = evidencePolicy;
        SkipPolicy = skipPolicy;
        _tags = NormalizeTags(tags);
        ExternalRef = Normalize(externalRef);
        Status = ProcessStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>Natural key of the process within the tenant ('PRC-VEN-A30'). Stable across versions (W13).</summary>
    public string Code { get; private set; }

    public string Name { get; private set; }

    public ProcessProfile Profile { get; private set; }

    /// <summary>The published version in force, if any. Null means "nothing executable right now".</summary>
    public Guid? CurrentVersionId { get; private set; }

    /// <summary>Expected output: product (repetitive) or typified deliverable (project). Logical reference to <c>master.items</c>.</summary>
    public Guid? OutputItemId { get; private set; }

    /// <summary>Logical reference to <c>master.uom</c> — no physical foreign key.</summary>
    public Guid? OutputUomId { get; private set; }

    /// <summary>SUGGESTED physical scope (Layer 1), never mandatory (CB11). Logical reference to <c>config.sites</c>.</summary>
    public Guid? SiteId { get; private set; }

    /// <summary>Logical reference to <c>config.areas</c> — no physical foreign key.</summary>
    public Guid? AreaId { get; private set; }

    /// <summary>Logical reference to <c>config.lines</c> — no physical foreign key.</summary>
    public Guid? LineId { get; private set; }

    /// <summary>Default evidence policy of its tasks; a task may override it.</summary>
    public EvidencePolicy EvidencePolicy { get; private set; }

    public SkipPolicy SkipPolicy { get; private set; }

    /// <summary>Free classification for the process library.</summary>
    public IReadOnlyCollection<string> Tags => _tags;

    /// <summary>Correlation with the ERP route/BOM — a suggestion only: the process is never outsourced.</summary>
    public string? ExternalRef { get; private set; }

    public ProcessStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    public bool IsArchived => Status == ProcessStatus.Archived;

    /// <summary>Whether a version of this process is published and therefore executable.</summary>
    public bool HasPublishedVersion => CurrentVersionId is not null;

    /// <exception cref="ArgumentException">When the code or the name are empty.</exception>
    public static Process Create(
        string code,
        string name,
        ProcessProfile profile,
        Guid? outputItemId = null,
        Guid? outputUomId = null,
        Guid? siteId = null,
        Guid? areaId = null,
        Guid? lineId = null,
        EvidencePolicy evidencePolicy = EvidencePolicy.Recommended,
        SkipPolicy skipPolicy = SkipPolicy.Authorized,
        IEnumerable<string>? tags = null,
        string? externalRef = null)
        => new(
            UuidV7.NewGuid(),
            code,
            name,
            profile,
            outputItemId,
            outputUomId,
            siteId,
            areaId,
            lineId,
            evidencePolicy,
            skipPolicy,
            tags,
            externalRef);

    /// <summary>Creates the first draft version (1.0) of this process.</summary>
    public Result<ProcessVersion> StartInitialVersion(string? changeReason = null)
    {
        if (IsArchived)
        {
            return Result<ProcessVersion>.Failure(WorkModelErrors.ProcessArchivedConflict(Code));
        }

        return Result<ProcessVersion>.Success(ProcessVersion.CreateInitialDraft(Id, Profile, changeReason));
    }

    /// <summary>Derives a new draft version from an existing one (the way a published version evolves, W10).</summary>
    public Result<ProcessVersion> DeriveVersion(ProcessVersion source, VersionBump bump, string? changeReason = null)
    {
        if (IsArchived)
        {
            return Result<ProcessVersion>.Failure(WorkModelErrors.ProcessArchivedConflict(Code));
        }

        if (source.ProcessId != Id)
        {
            return Result<ProcessVersion>.Failure(WorkModelErrors.VersionBelongsToAnotherProcessInvalid);
        }

        return Result<ProcessVersion>.Success(ProcessVersion.DeriveDraft(source, bump, changeReason));
    }

    /// <summary>
    /// Publishes a version of this process. <b>CB15: one single published version per process</b> —
    /// the invariant lives here (and is mirrored by the partial unique index
    /// <c>ux_process_versions_published</c>), because the version alone cannot see its siblings.
    /// </summary>
    public Result PublishVersion(ProcessVersion version)
    {
        if (version.ProcessId != Id)
        {
            return Result.Failure(WorkModelErrors.VersionBelongsToAnotherProcessInvalid);
        }

        if (IsArchived)
        {
            return Result.Failure(WorkModelErrors.ProcessArchivedConflict(Code));
        }

        if (CurrentVersionId is not null && CurrentVersionId != version.Id)
        {
            return Result.Failure(WorkModelErrors.PublishedVersionAlreadyExistsConflict(Code));
        }

        var published = version.Publish();
        if (published.IsFailure)
        {
            return published;
        }

        CurrentVersionId = version.Id;
        Touch();

        return Result.Success();
    }

    /// <summary>
    /// Suspends the version in force: running executions continue, new ones are blocked. The process
    /// is left with no version in force, so another one may be published.
    /// </summary>
    public Result SuspendVersion(ProcessVersion version, string? reason = null)
    {
        if (version.ProcessId != Id)
        {
            return Result.Failure(WorkModelErrors.VersionBelongsToAnotherProcessInvalid);
        }

        var suspended = version.Suspend(reason);
        if (suspended.IsFailure)
        {
            return suspended;
        }

        if (CurrentVersionId == version.Id)
        {
            CurrentVersionId = null;
        }

        Touch();

        return Result.Success();
    }

    /// <summary>Updates the editable descriptive attributes of the process.</summary>
    public void Update(
        string name,
        Guid? outputItemId,
        Guid? outputUomId,
        Guid? siteId,
        Guid? areaId,
        Guid? lineId,
        EvidencePolicy evidencePolicy,
        SkipPolicy skipPolicy,
        IEnumerable<string>? tags)
    {
        Name = NormalizeRequired(name, "name");
        OutputItemId = outputItemId;
        OutputUomId = outputUomId;
        SiteId = siteId;
        AreaId = areaId;
        LineId = lineId;
        EvidencePolicy = evidencePolicy;
        SkipPolicy = skipPolicy;
        _tags = NormalizeTags(tags);
        Touch();
    }

    /// <summary>Logical removal: never a physical delete while executions reference the process (R4).</summary>
    public void Archive()
    {
        Status = ProcessStatus.Archived;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private static string[] NormalizeTags(IEnumerable<string>? tags)
        => (tags ?? Enumerable.Empty<string>())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"'{paramName}' is required and cannot be empty.", paramName);
        }

        return value.Trim();
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
