using Nexo.BuildingBlocks.Domain;

namespace Nexo.Execution.Domain;

/// <summary>
/// A piece of evidence as a first-class business artefact (<c>execution.evidence</c>, §2.8). It is the
/// proof that a requirement of Layer 2 is satisfied — not a decorative attachment.
/// </summary>
/// <remarks>
/// <b>The binary does not live here.</b> <see cref="FileId"/> references the object in Files/Media
/// (<c>platform.files</c>, the S3 metadata); this row is the artefact that <i>satisfies</i> a task's
/// evidence requirement, carries its integrity hash and links to the task run and to the fact in the
/// event store. Being offline-first, evidence is captured <see cref="EvidenceStatus.Pending"/> (only the
/// reference) and materialized later (contract §2.7). Child entity of the <see cref="Execution"/> aggregate.
/// </remarks>
public sealed class Evidence : Entity<Guid>
{
    // EF Core materialization constructor.
    private Evidence()
    {
    }

    private Evidence(
        Guid id,
        Guid executionId,
        Guid? taskRunId,
        EvidenceKind kind,
        EvidenceStatus status,
        Guid? requirementId,
        Guid? fileId,
        string? mediaRef,
        byte[]? contentHash,
        bool isMandatory,
        Guid? capturedBy,
        string? caption)
        : base(id)
    {
        ExecutionId = executionId;
        TaskRunId = taskRunId;
        Kind = kind;
        Status = status;
        RequirementId = requirementId;
        FileId = fileId;
        MediaRef = Normalize(mediaRef);
        ContentHash = contentHash;
        HashAlgo = "sha256";
        IsMandatory = isMandatory;
        CapturedBy = capturedBy;
        Caption = Normalize(caption);
        CapturedAt = DateTimeOffset.UtcNow;
        CreatedAt = CapturedAt;
        UpdatedAt = CapturedAt;
    }

    public Guid ExecutionId { get; private set; }

    /// <summary>The task run the evidence proves; <c>null</c> for execution-level evidence.</summary>
    public Guid? TaskRunId { get; private set; }

    public EvidenceKind Kind { get; private set; }

    public EvidenceStatus Status { get; private set; }

    /// <summary>Logical reference to <c>work.task_evidence_requirements</c> — what was required.</summary>
    public Guid? RequirementId { get; private set; }

    /// <summary>Logical reference to <c>platform.files</c> (the object in S3) — no physical foreign key.</summary>
    public Guid? FileId { get; private set; }

    /// <summary>Opaque media reference used offline before the file is materialized.</summary>
    public string? MediaRef { get; private set; }

    /// <summary>Content hash for integrity / non-repudiation (same criterion as the event store).</summary>
    public byte[]? ContentHash { get; private set; }

    public string HashAlgo { get; private set; } = "sha256";

    /// <summary>Copy of the effective obligation at capture time.</summary>
    public bool IsMandatory { get; private set; }

    public Guid? CapturedBy { get; private set; }

    public string? Caption { get; private set; }

    public DateTimeOffset CapturedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    /// <summary>True once the referenced binary has landed (or its hash was verified).</summary>
    public bool IsMaterialized => Status is EvidenceStatus.Materialized or EvidenceStatus.Verified;

    internal static Evidence Create(
        Guid executionId,
        Guid? taskRunId,
        EvidenceKind kind,
        EvidenceStatus status,
        Guid? requirementId,
        Guid? fileId,
        string? mediaRef,
        byte[]? contentHash,
        bool isMandatory,
        Guid? capturedBy,
        string? caption)
        => new(
            UuidV7.NewGuid(),
            executionId,
            taskRunId,
            kind,
            status,
            requirementId,
            fileId,
            mediaRef,
            contentHash,
            isMandatory,
            capturedBy,
            caption);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
