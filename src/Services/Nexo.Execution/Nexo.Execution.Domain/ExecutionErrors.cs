using Nexo.BuildingBlocks.Domain;

namespace Nexo.Execution.Domain;

/// <summary>
/// Canonical domain errors of the execution engine. The API maps the code suffix to an HTTP status
/// (<c>NotFound</c> → 404, <c>Conflict</c> → 409, <c>Invalid</c> → 422), so the suffixes matter.
/// </summary>
public static class ExecutionErrors
{
    // --- Creation / flavour -------------------------------------------------------------------

    public static Error CodeRequiredInvalid => new(
        "Execution.CodeRequiredInvalid",
        "The execution code is required and cannot be empty.");

    public static Error SnapshotEmptyInvalid => new(
        "Execution.SnapshotEmptyInvalid",
        "The process snapshot has no tasks: there is nothing to instantiate.");

    /// <summary>E4: a batch run needs a product and a target quantity.</summary>
    public static Error BatchTargetRequiredInvalid => new(
        "Execution.BatchTargetRequiredInvalid",
        "A batch execution requires a target product and a positive target quantity (E4).");

    /// <summary>E5: a project run needs a deliverable and a committed date.</summary>
    public static Error ProjectCommitmentRequiredInvalid => new(
        "Execution.ProjectCommitmentRequiredInvalid",
        "A project execution requires a deliverable and a committed date (E5).");

    /// <summary>W15: a project never declares a target output quantity.</summary>
    public static Error ProjectTargetNotAllowedInvalid => new(
        "Execution.ProjectTargetNotAllowedInvalid",
        "A project execution does not declare a target output quantity (W15).");

    // --- Lookups ------------------------------------------------------------------------------

    public static Error ExecutionNotFound(string reference) => new(
        "Execution.NotFound",
        $"Execution '{reference}' was not found.");

    public static Error TaskRunNotFound(string reference) => new(
        "Execution.TaskRun.NotFound",
        $"Task run '{reference}' was not found in this execution.");

    // --- Execution lifecycle ------------------------------------------------------------------

    public static Error ExecutionNotActiveConflict(ExecutionStatus status) => new(
        "Execution.NotActiveConflict",
        $"The execution is '{status.ToString().ToLowerInvariant()}' and does not admit operational changes.");

    public static Error ExecutionAlreadyClosedConflict => new(
        "Execution.AlreadyClosedConflict",
        "The execution is already closed or cancelled.");

    /// <summary>The close checklist rejects a run with mandatory task runs still open.</summary>
    public static Error MandatoryTasksOpenConflict(IReadOnlyCollection<string> openTasks) => new(
        "Execution.MandatoryTasksOpenConflict",
        $"The execution cannot be closed: mandatory tasks are still open: {string.Join(", ", openTasks)}.");

    // --- Task run lifecycle -------------------------------------------------------------------

    /// <summary>E6/E7: predecessors incomplete (or the lag has not expired) — the task is not ready.</summary>
    public static Error TaskNotReadyConflict(string reference, IReadOnlyCollection<string> pendingPredecessors) => new(
        "Execution.TaskRun.NotReadyConflict",
        $"Task run '{reference}' cannot start: predecessors are not satisfied yet: {string.Join(", ", pendingPredecessors)} (E6/E7).");

    public static Error TaskNotInProgressConflict(string reference, TaskRunStatus status) => new(
        "Execution.TaskRun.NotInProgressConflict",
        $"Task run '{reference}' is '{status.ToString().ToLowerInvariant()}'; the operation requires it to be in progress.");

    public static Error TaskAlreadyTerminalConflict(string reference, TaskRunStatus status) => new(
        "Execution.TaskRun.AlreadyTerminalConflict",
        $"Task run '{reference}' is already terminal ('{status.ToString().ToLowerInvariant()}') and cannot change.");

    public static Error TaskNotBlockedConflict(string reference) => new(
        "Execution.TaskRun.NotBlockedConflict",
        $"Task run '{reference}' is not blocked, so it cannot be unblocked.");

    /// <summary>FF precedence: a task cannot finish before its finish→finish predecessors do.</summary>
    public static Error FinishPredecessorsOpenConflict(string reference, IReadOnlyCollection<string> openPredecessors) => new(
        "Execution.TaskRun.FinishPredecessorsOpenConflict",
        $"Task run '{reference}' cannot complete while its finish→finish predecessors are open: {string.Join(", ", openPredecessors)}.");

    /// <summary>E11: the mandatory evidence of the task is missing.</summary>
    public static Error MandatoryEvidenceMissingConflict(string reference, EvidenceKind kind, int required, int present) => new(
        "Execution.TaskRun.MandatoryEvidenceMissingConflict",
        $"Task run '{reference}' requires at least {required} piece(s) of '{kind.ToString().ToLowerInvariant()}' evidence to complete; {present} attached (E11).");

    /// <summary>E18: skipping a mandatory task needs an explicit authorization.</summary>
    public static Error MandatorySkipUnauthorizedConflict(string reference) => new(
        "Execution.TaskRun.MandatorySkipUnauthorizedConflict",
        $"Task run '{reference}' is mandatory; skipping it requires an authorization (E18).");

    // --- Quantities ---------------------------------------------------------------------------

    public static Error ProgressOutOfRangeInvalid => new(
        "Execution.Progress.OutOfRangeInvalid",
        "Progress percentage must be between 0 and 100.");

    public static Error NegativeQuantityInvalid => new(
        "Execution.Quantity.NegativeInvalid",
        "A reported quantity cannot be negative.");

    public static Error ConsumptionQuantityInvalid => new(
        "Execution.Consumption.QuantityInvalid",
        "A real consumption must declare a quantity greater than zero.");

    // --- Evidence -----------------------------------------------------------------------------

    public static Error EvidencePayloadMissingInvalid => new(
        "Execution.Evidence.PayloadMissingInvalid",
        "Evidence must carry a media reference, structured form data or a reading reference.");
}
