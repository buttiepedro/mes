namespace Nexo.Production.Api;

/// <summary>Request body for <c>POST /v1/production/records</c>.</summary>
public sealed record RegisterProductionRequest(
    Guid RunId,
    decimal GoodQty,
    decimal ScrapQty,
    Guid OperatorId,
    string Source);
