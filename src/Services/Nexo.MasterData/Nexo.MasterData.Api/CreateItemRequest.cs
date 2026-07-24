namespace Nexo.MasterData.Api;

/// <summary>
/// Request body for <c>POST /v1/items</c>. Absolute floor: code + name + base unit.
/// <c>Roles</c> carries <c>product</c>, <c>input</c> or both (a semi-finished item).
/// </summary>
public sealed record CreateItemRequest(
    string Code,
    string Name,
    string BaseUom,
    IReadOnlyList<string>? Roles = null,
    string Tracking = "none",
    string? Category = null,
    string? Family = null,
    decimal? IdealCycleTime = null,
    Guid? DefaultProcessId = null,
    string? QualitySpecs = null,
    string? ExternalRef = null);
