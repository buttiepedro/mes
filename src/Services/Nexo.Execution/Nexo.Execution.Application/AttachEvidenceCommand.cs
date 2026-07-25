using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// Attaches (or materializes) evidence on a task run (<c>POST /tasks/{id}/evidence</c>) — cancels evidence
/// debt. Offline-first: <see cref="Status"/> may be <c>pending</c> and be materialized later. The binary
/// is referenced, never inlined. <see cref="ContentHash"/> is a hex string (converted to bytes).
/// </summary>
public sealed record AttachEvidenceCommand(
    Guid TaskRunId,
    string Kind,
    string Status = "pending",
    Guid? FileId = null,
    string? MediaRef = null,
    string? ContentHash = null,
    Guid? RequirementId = null,
    Guid? CapturedBy = null,
    string? Caption = null) : ICommand<EvidenceDto>;
