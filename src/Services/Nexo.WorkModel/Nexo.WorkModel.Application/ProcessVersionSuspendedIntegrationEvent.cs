using Nexo.BuildingBlocks.Messaging;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Public contract published to the backbone when the version in force is suspended.
/// Canonical type: <c>nexo.process.version_suspended</c>. <b>Execution</b> blocks new
/// instantiations; running executions continue with the frozen version.
/// </summary>
public sealed record ProcessVersionSuspendedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Process_VersionSuspended;

    public Guid ProcessId { get; init; }

    public Guid VersionId { get; init; }

    public string VersionNo { get; init; } = string.Empty;

    public string? Reason { get; init; }
}
