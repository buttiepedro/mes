using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Domain;

/// <summary>
/// Standard consumption declared by a task (<c>work.task_inputs</c>).
/// </summary>
/// <remarks>
/// <b>The input is declared on the TASK, not on the process.</b> The process-level "bill of materials"
/// is a derived view. That temporal granularity — what each task consumes and <i>when</i> — is exactly
/// what an ERP BOM does not have (docs/design/03-data-schema.md §2.6.4).
/// <para>
/// <see cref="ItemId"/> points at <c>master.items</c> and <see cref="UomId"/> at <c>master.uom</c>, both
/// owned by Nexo.MasterData: they are <b>logical references without a physical foreign key</b>, so the
/// migrations of the two bounded contexts stay independent even though the tenant's services share one
/// physical database (§1.9).
/// </para>
/// </remarks>
public sealed class TaskInput : Entity<Guid>
{
    // EF Core materialization constructor.
    private TaskInput()
    {
    }

    private TaskInput(
        Guid id,
        Guid taskId,
        Guid processVersionId,
        TaskInputSpec spec)
        : base(id)
    {
        TaskId = taskId;
        ProcessVersionId = processVersionId;
        ItemId = spec.ItemId;
        Quantity = spec.Quantity;
        UomId = spec.UomId;
        Basis = spec.Basis;
        Kind = spec.Kind;
        TolerancePct = spec.TolerancePct;
        IsBlocking = spec.IsBlocking;
        RequiresTraceability = spec.RequiresTraceability;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid TaskId { get; private set; }

    /// <summary>Denormalized: keeps every row of the graph anchored to a single version (G4).</summary>
    public Guid ProcessVersionId { get; private set; }

    /// <summary>Logical reference to <c>master.items</c> — no physical foreign key.</summary>
    public Guid ItemId { get; private set; }

    /// <summary>Standard (theoretical) quantity. Always greater than zero.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Logical reference to <c>master.uom</c> — no physical foreign key.</summary>
    public Guid UomId { get; private set; }

    public InputBasis Basis { get; private set; }

    public InputKind Kind { get; private set; }

    /// <summary>Deviation accepted before the execution raises an alert (E14).</summary>
    public decimal? TolerancePct { get; private set; }

    /// <summary>Its absence prevents the task from starting.</summary>
    public bool IsBlocking { get; private set; }

    /// <summary>Requires registering the consumed batch/serial (E15).</summary>
    public bool RequiresTraceability { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    internal static TaskInput Create(Guid taskId, Guid processVersionId, TaskInputSpec spec)
        => new(UuidV7.NewGuid(), taskId, processVersionId, spec);

    /// <summary>Copy of this input for a derived draft version (new identity, same content).</summary>
    internal TaskInput CopyTo(Guid taskId, Guid processVersionId) => new(
        UuidV7.NewGuid(),
        taskId,
        processVersionId,
        new TaskInputSpec(ItemId, Quantity, UomId, Basis, Kind, TolerancePct, IsBlocking, RequiresTraceability));

    /// <summary>Validates an input spec in isolation; returns <c>null</c> when it is well formed.</summary>
    internal static string? Validate(TaskInputSpec spec)
    {
        if (spec.ItemId == Guid.Empty)
        {
            return "an input must reference an item of master.items.";
        }

        if (spec.UomId == Guid.Empty)
        {
            return "an input must reference a unit of measure of master.uom.";
        }

        if (spec.Quantity <= 0m)
        {
            return "the standard quantity of an input must be greater than zero.";
        }

        if (spec.TolerancePct is < 0m)
        {
            return "the tolerance of an input cannot be negative.";
        }

        return null;
    }
}
