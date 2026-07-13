namespace Nexo.Production.Domain;

/// <summary>Lifecycle of a <see cref="WorkOrder"/> (mirror of the Odoo Manufacturing Order).</summary>
public enum WorkOrderStatus
{
    Planned = 0,
    Released = 1,
    InProgress = 2,
    Done = 3,
    Closed = 4
}

/// <summary>Lifecycle of a <see cref="ProductionRun"/>.</summary>
public enum RunStatus
{
    Open = 0,
    Closed = 1
}

/// <summary>Origin of a <see cref="ProductionRecord"/>.</summary>
public enum ProductionSource
{
    Manual = 0,
    Datalogger = 1
}
