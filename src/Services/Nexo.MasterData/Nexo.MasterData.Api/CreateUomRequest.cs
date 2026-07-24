namespace Nexo.MasterData.Api;

/// <summary>Request body for <c>POST /v1/uoms</c>.</summary>
public sealed record CreateUomRequest(
    string Code,
    string Name,
    string Symbol,
    string Magnitude,
    decimal FactorToBase,
    bool IsBase = false,
    short Decimals = 4,
    string? ExternalRef = null);
