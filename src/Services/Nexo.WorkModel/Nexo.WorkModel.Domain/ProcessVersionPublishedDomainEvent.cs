using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Domain;

/// <summary>
/// Raised when a version becomes published: from this moment it is immutable and Execution may
/// instantiate it. Translated by the Application layer to the canonical integration event
/// <c>nexo.process.version_published</c>.
/// </summary>
public sealed record ProcessVersionPublishedDomainEvent(
    Guid ProcessId,
    Guid VersionId,
    string VersionNo,
    ProcessProfile Profile,
    int TaskCount,
    decimal? WorkloadSeconds) : DomainEvent;
