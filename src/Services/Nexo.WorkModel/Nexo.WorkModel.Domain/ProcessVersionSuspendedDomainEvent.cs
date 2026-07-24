using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Domain;

/// <summary>
/// Raised when the version in force is suspended: running executions continue, new ones are blocked.
/// Translated by the Application layer to the canonical integration event
/// <c>nexo.process.version_suspended</c>.
/// </summary>
public sealed record ProcessVersionSuspendedDomainEvent(
    Guid ProcessId,
    Guid VersionId,
    string VersionNo,
    string? Reason) : DomainEvent;
