using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Domain;

/// <summary>
/// Canonical domain errors of the work model. The API maps the code to an HTTP status
/// (<c>NotFound</c> → 404, <c>Conflict</c> → 409, <c>Invalid</c> → 422), so the suffixes matter.
/// </summary>
public static class WorkModelErrors
{
    // --- Process ------------------------------------------------------------------------------

    public static Error ProcessArchivedConflict(string code) => new(
        "WorkModel.Process.ArchivedConflict",
        $"Process '{code}' is archived and does not admit new versions.");

    public static Error PublishedVersionAlreadyExistsConflict(string code) => new(
        "WorkModel.Process.PublishedVersionAlreadyExistsConflict",
        $"Process '{code}' already has a published version. Suspend it before publishing another one (CB15).");

    public static Error VersionBelongsToAnotherProcessInvalid => new(
        "WorkModel.Process.VersionBelongsToAnotherProcessInvalid",
        "The version does not belong to this process.");

    // --- Version lifecycle --------------------------------------------------------------------

    /// <summary>W10: a published version is never edited — a new draft is derived from it.</summary>
    public static Error VersionNotEditableConflict(string versionNo, ProcessVersionState state) => new(
        "WorkModel.Version.NotEditableConflict",
        $"Version '{versionNo}' is in state '{state.ToString().ToLowerInvariant()}' and does not admit structural changes (W10). Derive a new draft from it.");

    public static Error VersionNotDraftConflict(string versionNo, ProcessVersionState state) => new(
        "WorkModel.Version.NotDraftConflict",
        $"Only a draft version can be published; version '{versionNo}' is '{state.ToString().ToLowerInvariant()}'.");

    public static Error VersionNotPublishedConflict(string versionNo, ProcessVersionState state) => new(
        "WorkModel.Version.NotPublishedConflict",
        $"Only a published version can be suspended; version '{versionNo}' is '{state.ToString().ToLowerInvariant()}'.");

    public static Error VersionValidationInvalid(IReadOnlyCollection<ProcessVersionValidationIssue> blocking) => new(
        "WorkModel.Version.ValidationInvalid",
        $"The version does not pass the blocking validations: {string.Join(" | ", blocking.Select(issue => $"{issue.Rule}: {issue.Detail}"))}");

    // --- Tasks --------------------------------------------------------------------------------

    public static Error TaskCodeRequiredInvalid => new(
        "WorkModel.Task.CodeRequiredInvalid",
        "The task code is required and cannot be empty.");

    public static Error TaskNameRequiredInvalid => new(
        "WorkModel.Task.NameRequiredInvalid",
        "The task name is required and cannot be empty.");

    public static Error TaskCodeConflict(string code) => new(
        "WorkModel.Task.CodeConflict",
        $"A task with code '{code}' already exists in this version.");

    public static Error TaskNotFound(string reference) => new(
        "WorkModel.Task.NotFound",
        $"Task '{reference}' was not found in this version.");

    public static Error TaskRoleRequiredInvalid(string code) => new(
        "WorkModel.Task.RoleRequiredInvalid",
        $"Task '{code}' must declare a responsible role (W3).");

    public static Error TaskWeightInvalid(string code) => new(
        "WorkModel.Task.WeightInvalid",
        $"The progress weight of task '{code}' must be between 0 and 100.");

    public static Error TaskDurationInvalid(string code) => new(
        "WorkModel.Task.DurationInvalid",
        $"The durations of task '{code}' must be greater than zero when supplied (W7).");

    public static Error TaskInputInvalid(string code, string detail) => new(
        "WorkModel.Task.InputInvalid",
        $"Task '{code}': {detail}");

    // --- Graph --------------------------------------------------------------------------------

    /// <summary>B1: trivial cycle of length 1 — a task cannot precede itself.</summary>
    public static Error SelfDependencyInvalid(string code) => new(
        "WorkModel.Graph.SelfDependencyInvalid",
        $"Task '{code}' cannot precede itself (G1, trivial edge).");

    public static Error DuplicateEdgeInvalid(string from, string to) => new(
        "WorkModel.Graph.DuplicateEdgeInvalid",
        $"The precedence '{from}' -> '{to}' is declared more than once (the graph is not a multigraph).");

    public static Error NegativeLagInvalid(string from, string to) => new(
        "WorkModel.Graph.NegativeLagInvalid",
        $"The lag of the precedence '{from}' -> '{to}' cannot be negative (G5; negative lag is deferred to V1).");

    /// <summary>G1: the graph of a version must be acyclic; the detected cycle travels in the message.</summary>
    public static Error CycleInvalid(IReadOnlyList<string> cycle) => new(
        "WorkModel.Graph.CycleInvalid",
        $"The precedences close a cycle in the DAG (G1): {string.Join(" -> ", cycle)}.");
}
