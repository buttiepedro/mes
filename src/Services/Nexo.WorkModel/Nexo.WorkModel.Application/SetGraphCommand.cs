using Nexo.BuildingBlocks.Application;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Replaces the whole precedence set of a draft version (<c>PUT /versions/{id}/graph</c>).
/// Returns 422 with the detected cycle when the proposed graph is not a DAG (G1).
/// </summary>
/// <remarks>
/// The edges travel with the task <b>codes</b>, which is what the editor and the contract speak; the
/// aggregate resolves them to identifiers and rejects any code that does not belong to the version (G4).
/// </remarks>
public sealed record SetGraphCommand(
    Guid ProcessId,
    Guid VersionId,
    IReadOnlyList<GraphEdgeRequest> Edges) : ICommand<ProcessVersionGraphDto>;

/// <summary>One precedence: predecessor code, successor code, kind (FS|SS|FF) and lag in seconds.</summary>
public sealed record GraphEdgeRequest(
    string FromTask,
    string ToTask,
    string Kind = "FS",
    int LagSeconds = 0);
