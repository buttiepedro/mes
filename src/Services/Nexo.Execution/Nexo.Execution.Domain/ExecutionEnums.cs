namespace Nexo.Execution.Domain;

/// <summary>
/// Flavour of an <see cref="Execution"/> (docs/design/03-data-schema.md §2.7,
/// <c>nexo.execution_flavor_enum</c>). It <b>derives</b> from the process profile (E3): a
/// <c>repetitive</c> process instantiates a <see cref="Batch"/>, a <c>project</c> process a
/// <see cref="Project"/>. It is the only attribute that structurally tells the two apart at birth:
/// the engine, the DAG and the clock are identical for both.
/// </summary>
public enum ExecutionFlavor
{
    Batch = 0,
    Project = 1
}

/// <summary>
/// Lifecycle of an <see cref="Execution"/> (the <c>ck_exec_status</c> check of §2.7.1).
/// </summary>
/// <remarks>
/// The MVP slice drives the states that carry behaviour here — <see cref="Released"/> at birth (the
/// separate <c>:schedule</c>/<c>:release</c> steps are folded into instantiation), <see cref="InProgress"/>
/// on the first started task, and the terminal <see cref="Closed"/>/<see cref="Cancelled"/>. The rest
/// (<c>scheduled</c>, <c>verified</c>, <c>synced</c>, <c>archived</c>, <c>reopened</c>, ...) are declared
/// so the wire vocabulary matches the DDL, and are reached by slices out of scope here.
/// </remarks>
public enum ExecutionStatus
{
    Draft = 0,
    Scheduled = 1,
    Released = 2,
    InProgress = 3,
    Paused = 4,
    Blocked = 5,
    Rescheduled = 6,
    Completed = 7,
    Closed = 8,
    Verified = 9,
    Synced = 10,
    Archived = 11,
    Cancelled = 12,
    Reopened = 13
}

/// <summary>
/// Lifecycle of an instantiated task (<c>ck_task_runs_status</c> of §2.7.2). A run walks from
/// <see cref="Pending"/> (waiting on the DAG) to <see cref="Ready"/> (predecessors satisfied) and on
/// to a terminal <see cref="Completed"/>/<see cref="Skipped"/>.
/// </summary>
public enum TaskRunStatus
{
    Pending = 0,
    Ready = 1,
    Assigned = 2,
    InProgress = 3,
    Paused = 4,
    Blocked = 5,
    InControl = 6,
    NonConforming = 7,
    Rework = 8,
    Completed = 9,
    Skipped = 10,
    Rejected = 11,
    Cancelled = 12,
    Reopened = 13
}

/// <summary>
/// What originated the execution (<c>ck_exec_trigger</c> of §2.7.1). Polymorphic reference: the
/// trigger may be external (an ERP work order) or absent (a manual start). The <c>manual</c> trigger
/// is always available, even without an ERP connector.
/// </summary>
public enum TriggerKind
{
    WorkOrder = 0,
    Plan = 1,
    Stock = 2,
    Rule = 3,
    Contract = 4,
    Quote = 5,
    Maintenance = 6,
    Manual = 7
}

/// <summary>
/// Kind of precedence between two instantiated tasks, frozen from the process version's DAG.
/// <c>FS</c> finish→start, <c>SS</c> start→start, <c>FF</c> finish→finish.
/// </summary>
public enum DependencyType
{
    FS = 0,
    SS = 1,
    FF = 2
}

/// <summary>How a task run's work is assigned (<c>ck_task_runs_mode</c> of §2.7.2).</summary>
public enum AssignmentMode
{
    Individual = 0,
    Crew = 1,
    RoleOpen = 2,
    Automatic = 3,
    External = 4
}

/// <summary>Method used to declare a task run's progress; it always travels with the value.</summary>
public enum ProgressMethod
{
    Declared = 0,
    Quantity = 1,
    Checklist = 2,
    Time = 3,
    Signal = 4
}

/// <summary>How a real input consumption was captured (<c>ck_ic_method</c> of §2.7.3).</summary>
public enum ConsumptionMethod
{
    Declared = 0,
    Backflush = 1,
    Scale = 2,
    Scan = 3,
    Adjustment = 4
}

/// <summary>Whether the task must be executed, may be skipped, or depends on a run parameter.</summary>
public enum TaskObligation
{
    Mandatory = 0,
    Optional = 1,
    Conditional = 2
}

/// <summary>Kind of evidence (<c>ck_ev_kind</c> of §2.8). The binary never lives here — it is referenced.</summary>
public enum EvidenceKind
{
    Photo = 0,
    File = 1,
    SensorReading = 2,
    Signature = 3,
    Video = 4,
    Form = 5
}

/// <summary>
/// Materialization state of a piece of evidence (offline-first, contract §2.7). It is captured
/// <see cref="Pending"/> (only the reference), later <see cref="Materialized"/> when the binary lands
/// in Files/Media, and finally <see cref="Verified"/> when its content hash checks out.
/// </summary>
public enum EvidenceStatus
{
    Pending = 0,
    Materialized = 1,
    Verified = 2
}

/// <summary>Cause of a task block — the direct input of the bottleneck KPI.</summary>
public enum BlockCause
{
    Input = 0,
    Resource = 1,
    Approval = 2,
    Quality = 3
}

/// <summary>How an execution was closed (<c>close_kind</c> of §2.7.1).</summary>
public enum CloseKind
{
    Normal = 0,
    Partial = 1,
    Forced = 2,
    Cancelled = 3,
    Expired = 4
}
