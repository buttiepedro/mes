using Nexo.BuildingBlocks.Domain;

namespace Nexo.Production.Domain;

/// <summary>
/// Production order (mirror of the Odoo Manufacturing Order). Aggregate root.
/// </summary>
public sealed class WorkOrder : AggregateRoot<Guid>
{
    // EF Core materialization constructor.
    private WorkOrder()
    {
        Code = string.Empty;
        PlannedQty = Quantity.Zero;
    }

    private WorkOrder(Guid id, string code, Guid productId, Quantity plannedQty)
    {
        Id = id;
        Code = code;
        ProductId = productId;
        PlannedQty = plannedQty;
        Status = WorkOrderStatus.Planned;
    }

    public string Code { get; private set; }

    public Guid ProductId { get; private set; }

    public Quantity PlannedQty { get; private set; }

    public WorkOrderStatus Status { get; private set; }

    public static WorkOrder Create(string code, Guid productId, Quantity plannedQty)
        => new(UuidV7.NewGuid(), code, productId, plannedQty);

    public void Release()
    {
        if (Status != WorkOrderStatus.Planned)
        {
            throw new InvalidOperationException($"Only a planned work order can be released (current: {Status}).");
        }

        Status = WorkOrderStatus.Released;
    }

    public void Start()
    {
        if (Status is not (WorkOrderStatus.Released or WorkOrderStatus.Planned))
        {
            throw new InvalidOperationException($"Cannot start a work order in status {Status}.");
        }

        Status = WorkOrderStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != WorkOrderStatus.InProgress)
        {
            throw new InvalidOperationException($"Only a work order in progress can be completed (current: {Status}).");
        }

        Status = WorkOrderStatus.Done;
    }

    public void Close() => Status = WorkOrderStatus.Closed;
}
