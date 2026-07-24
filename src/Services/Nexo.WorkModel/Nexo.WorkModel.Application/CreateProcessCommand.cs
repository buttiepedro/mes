using Nexo.BuildingBlocks.Application;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Creates a process (identity stable across every version) together with its version 1.0 in draft,
/// exactly as <c>POST /v1/processes</c> promises in the contract (§2.6).
/// </summary>
/// <remarks>
/// <paramref name="OutputItemId"/> / <paramref name="OutputUomId"/> and the scope ids are logical
/// references to <c>master.*</c> / <c>config.*</c>: they are not resolved against those catalogs here,
/// because doing so would couple this bounded context to another service's schema (§1.9). The
/// consistency warning belongs to the editor (CB8) and to the publish-time validation.
/// </remarks>
public sealed record CreateProcessCommand(
    string Code,
    string Name,
    string Profile,
    Guid? OutputItemId = null,
    Guid? OutputUomId = null,
    Guid? SiteId = null,
    Guid? AreaId = null,
    Guid? LineId = null,
    string EvidencePolicy = "recommended",
    string SkipPolicy = "authorized",
    IReadOnlyList<string>? Tags = null,
    string? ExternalRef = null) : ICommand<ProcessCreatedDto>;
