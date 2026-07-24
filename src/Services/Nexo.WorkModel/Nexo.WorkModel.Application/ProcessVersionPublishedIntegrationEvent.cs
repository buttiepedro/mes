using Nexo.BuildingBlocks.Messaging;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Public contract published to the backbone when a process version is published.
/// Canonical type: <c>nexo.process.version_published</c>. From this event on, <b>Execution</b> may
/// instantiate the version (Dashboards, Traceability and Audit also consume it).
/// </summary>
public sealed record ProcessVersionPublishedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Process_VersionPublished;

    public Guid ProcessId { get; init; }

    public Guid VersionId { get; init; }

    public string VersionNo { get; init; } = string.Empty;

    /// <summary>repetitive | project.</summary>
    public string Profile { get; init; } = string.Empty;

    public int TaskCount { get; init; }

    /// <summary>Sum of standard durations (workload — <b>not</b> the elapsed duration of the DAG).</summary>
    public decimal? WorkloadSeconds { get; init; }
}
