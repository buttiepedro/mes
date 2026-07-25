using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

/// <summary>
/// Persistence port for the Execution slice (implemented by <c>ExecutionDbContext</c> in Infrastructure).
/// </summary>
/// <remarks>
/// The port is intentionally EF-free so the Application layer does not depend on Entity Framework
/// (Clean Architecture; the Application csproj references only MediatR + FluentValidation, mirroring
/// <c>IWorkModelDbContext</c>). Filtering and paging are expressed as explicit port operations instead of
/// leaking <c>IQueryable</c>. Every read excludes soft-deleted rows, and the execution reads bring the
/// <b>whole graph</b> (task runs with their frozen precedences, input consumptions and evidence) because
/// the aggregate cannot enforce the DAG and the close checklist over a graph it can only half see.
/// </remarks>
public interface IExecutionDbContext
{
    // --- Reads --------------------------------------------------------------------------------

    /// <summary>Loads an execution with its complete graph (task runs, precedences, consumptions, evidence).</summary>
    Task<Domain.Execution?> FindExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>Loads the execution that owns a given task run, with its complete graph.</summary>
    Task<Domain.Execution?> FindExecutionByTaskRunAsync(Guid taskRunId, CancellationToken cancellationToken = default);

    Task<bool> ExecutionCodeExistsAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Domain.Execution>> ListExecutionsAsync(
        ExecutionFlavor? flavor,
        ExecutionStatus? status,
        Guid? processId,
        DateTimeOffset? dueBefore,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Task runs whose work is not yet imputed to a person (the imputation backlog): completed or in-progress
    /// runs with no assignee. Each carries its owning execution so the read model can show the code.
    /// </summary>
    Task<IReadOnlyList<TaskRunImputationRow>> ListPendingImputationAsync(
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    // --- Writes -------------------------------------------------------------------------------

    void AddExecution(Domain.Execution execution);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Flat projection row for the imputation backlog (a task run joined to its execution header).</summary>
public sealed record TaskRunImputationRow(
    Guid TaskRunId,
    Guid ExecutionId,
    string ExecutionCode,
    string Flavor,
    string? Name,
    string Status,
    Guid? AssignedRoleId,
    long WorkedTimeSeconds,
    DateTimeOffset? ActualEndAt);
