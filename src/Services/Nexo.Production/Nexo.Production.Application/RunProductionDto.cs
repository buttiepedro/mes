namespace Nexo.Production.Application;

/// <summary>Read model for a run's consolidated production totals.</summary>
public sealed record RunProductionDto(
    Guid RunId,
    Guid WorkOrderId,
    decimal TotalGood,
    decimal TotalScrap,
    string Status,
    int RecordCount);
