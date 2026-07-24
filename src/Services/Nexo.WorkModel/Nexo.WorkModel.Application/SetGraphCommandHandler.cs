using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Application;

public sealed class SetGraphCommandHandler : ICommandHandler<SetGraphCommand, ProcessVersionGraphDto>
{
    private readonly IWorkModelDbContext _dbContext;

    public SetGraphCommandHandler(IWorkModelDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ProcessVersionGraphDto>> Handle(SetGraphCommand request, CancellationToken cancellationToken)
    {
        var edges = new List<TaskEdgeSpec>();

        foreach (var edge in request.Edges ?? Array.Empty<GraphEdgeRequest>())
        {
            if (!WorkModelWireValues.TryParseDependencyType(edge.Kind, out var type))
            {
                return Result<ProcessVersionGraphDto>.Failure(new Error(
                    "WorkModel.Graph.DependencyTypeInvalid",
                    $"Unknown dependency kind '{edge.Kind}'. Expected one of: FS, SS, FF."));
            }

            edges.Add(new TaskEdgeSpec(edge.FromTask, edge.ToTask, type, edge.LagSeconds));
        }

        var version = await _dbContext.FindVersionAsync(request.VersionId, cancellationToken);
        if (version is null || version.ProcessId != request.ProcessId)
        {
            return Result<ProcessVersionGraphDto>.Failure(new Error(
                "WorkModel.Version.NotFound",
                $"Version '{request.VersionId}' was not found for process '{request.ProcessId}'."));
        }

        // The aggregate is the one that rejects trivial edges, duplicates, negative lag and cycles.
        var applied = version.SetGraph(edges);
        if (applied.IsFailure)
        {
            return Result<ProcessVersionGraphDto>.Failure(applied.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ProcessVersionGraphDto>.Success(version.ToGraphDto());
    }
}
