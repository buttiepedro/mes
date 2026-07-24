namespace Nexo.WorkModel.Domain;

/// <summary>
/// Flavour of a <see cref="Process"/> (docs/design/03-data-schema.md §2.6,
/// <c>nexo.process_profile_enum</c>). It is the <b>only</b> attribute that tells "making windows"
/// apart from "building a site": the model and the engine are the same for both.
/// </summary>
public enum ProcessProfile
{
    Repetitive = 0,
    Project = 1
}

/// <summary>Lifecycle of a <see cref="Process"/>. Archiving is the only way out (R4).</summary>
public enum ProcessStatus
{
    Active = 0,
    Archived = 1
}

/// <summary>
/// Lifecycle of a <see cref="ProcessVersion"/>. Only <see cref="Draft"/> admits structural edits
/// (W10: a published version is immutable — a new draft is derived from it instead).
/// </summary>
/// <remarks>
/// The schema (§2.6.1) also lists <c>in_review</c>, <c>obsolete</c> and <c>discarded</c>. The MVP
/// slice implements the four states that carry behaviour; the rest are editorial and are deferred.
/// </remarks>
public enum ProcessVersionState
{
    Draft = 0,
    Published = 1,
    Suspended = 2,
    Archived = 3
}

/// <summary>
/// Kind of precedence between two tasks (MOD-18: the full DAG ships in the MVP).
/// <c>FS</c> finish→start, <c>SS</c> start→start, <c>FF</c> finish→finish.
/// </summary>
public enum DependencyType
{
    FS = 0,
    SS = 1,
    FF = 2
}

/// <summary>Completion criterion of a task (work-model.md §5.1).</summary>
public enum CompletionKind
{
    Declarative = 0,
    Quantity = 1,
    Measurement = 2,
    Signal = 3,
    Evidence = 4,
    Quality = 5,
    Approval = 6,
    Composite = 7
}

/// <summary>Whether the task must be executed, may be skipped, or depends on a run parameter.</summary>
public enum TaskObligation
{
    Mandatory = 0,
    Optional = 1,
    Conditional = 2
}

/// <summary>Evidence policy; a task inherits the process policy when it declares none.</summary>
public enum EvidencePolicy
{
    Mandatory = 0,
    Recommended = 1,
    Optional = 2,
    None = 3
}

/// <summary>Kind of evidence a task requires to be closed.</summary>
public enum EvidenceKind
{
    Photo = 0,
    File = 1,
    SensorReading = 2,
    Signature = 3,
    Video = 4,
    Form = 5
}

/// <summary>Whether an operator may skip a task of this process.</summary>
public enum SkipPolicy
{
    Allowed = 0,
    Authorized = 1,
    Forbidden = 2
}

/// <summary>How the standard quantity of a task input scales with the execution.</summary>
public enum InputBasis
{
    /// <summary>Proportional to the produced quantity.</summary>
    PerUnit = 0,

    /// <summary>Fixed per execution, regardless of the quantity.</summary>
    PerExecution = 1
}

/// <summary>Nature of a task input.</summary>
public enum InputKind
{
    Material = 0,
    Component = 1,
    Tool = 2,
    Service = 3,
    ExternalLabor = 4
}

/// <summary>Which segment of the version number a derived draft bumps (§9.4).</summary>
public enum VersionBump
{
    Major = 0,
    Minor = 1,
    Patch = 2
}

/// <summary>Whether a validation finding blocks publication or is only a warning.</summary>
public enum ValidationSeverity
{
    Blocking = 0,
    Warning = 1
}
