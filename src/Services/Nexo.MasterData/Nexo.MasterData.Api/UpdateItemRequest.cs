namespace Nexo.MasterData.Api;

/// <summary>Request body for <c>PUT /v1/items/{itemId}</c>. The code is the natural key and is not editable.</summary>
public sealed record UpdateItemRequest(
    string Name,
    IReadOnlyList<string>? Roles = null,
    string Tracking = "none",
    string? Category = null,
    string? Family = null,
    decimal? IdealCycleTime = null,
    Guid? DefaultProcessId = null,
    string? QualitySpecs = null);
