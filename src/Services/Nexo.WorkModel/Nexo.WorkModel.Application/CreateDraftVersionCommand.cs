using Nexo.BuildingBlocks.Application;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Derives a new draft version from the version in force (or from the newest one), copying its tasks,
/// inputs and precedences. This — never editing — is how a published version evolves (W10).
/// </summary>
/// <param name="Bump">major | minor | patch (§9.4).</param>
public sealed record CreateDraftVersionCommand(
    Guid ProcessId,
    string Bump = "minor",
    string? ChangeReason = null) : ICommand<ProcessVersionDto>;
