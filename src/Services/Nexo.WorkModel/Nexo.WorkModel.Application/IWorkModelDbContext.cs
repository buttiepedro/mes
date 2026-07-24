using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Persistence port for the Work Model slice (implemented by <c>WorkModelDbContext</c> in Infrastructure).
/// </summary>
/// <remarks>
/// The port is intentionally EF-free so the Application layer does not depend on Entity Framework
/// (Clean Architecture; the Application csproj references only MediatR + FluentValidation, mirroring
/// <c>IMasterDataDbContext</c>). Filtering and paging are therefore expressed as explicit port
/// operations instead of leaking <c>IQueryable</c>. Every read excludes soft-deleted rows, and the
/// version reads bring the <b>whole graph</b> (tasks, inputs and precedences) because the aggregate
/// cannot validate a DAG it can only half see.
/// </remarks>
public interface IWorkModelDbContext
{
    // --- Processes ----------------------------------------------------------------------------

    Task<Process?> FindProcessAsync(Guid processId, CancellationToken cancellationToken = default);

    Task<bool> ProcessCodeExistsAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Process>> ListProcessesAsync(
        ProcessProfile? profile,
        ProcessStatus? status,
        string? search,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    // --- Versions -----------------------------------------------------------------------------

    /// <summary>Loads a version with its complete graph (tasks, task inputs and precedences).</summary>
    Task<ProcessVersion?> FindVersionAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>Loads the published version of a process with its complete graph, if there is one.</summary>
    Task<ProcessVersion?> FindPublishedVersionAsync(Guid processId, CancellationToken cancellationToken = default);

    /// <summary>Loads the newest version of a process (by version number) with its complete graph.</summary>
    Task<ProcessVersion?> FindLatestVersionAsync(Guid processId, CancellationToken cancellationToken = default);

    /// <summary>Version history of a process, newest first. Does not bring the graph.</summary>
    Task<IReadOnlyList<ProcessVersion>> ListVersionsAsync(
        Guid processId,
        CancellationToken cancellationToken = default);

    Task<bool> VersionNumberExistsAsync(
        Guid processId,
        string versionNo,
        CancellationToken cancellationToken = default);

    // --- Writes -------------------------------------------------------------------------------

    void AddProcess(Process process);

    void AddVersion(ProcessVersion version);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
