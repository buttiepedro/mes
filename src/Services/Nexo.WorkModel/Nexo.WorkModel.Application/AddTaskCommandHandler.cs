using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Application;

public sealed class AddTaskCommandHandler : ICommandHandler<AddTaskCommand, WorkTaskDto>
{
    private readonly IWorkModelDbContext _dbContext;

    public AddTaskCommandHandler(IWorkModelDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<WorkTaskDto>> Handle(AddTaskCommand request, CancellationToken cancellationToken)
    {
        if (!WorkModelWireValues.TryParseCompletionKind(request.CompletionKind, out var completion))
        {
            return Result<WorkTaskDto>.Failure(new Error(
                "WorkModel.Task.CompletionKindInvalid",
                $"Unknown completion kind '{request.CompletionKind}'."));
        }

        if (!WorkModelWireValues.TryParseObligation(request.Obligation, out var obligation))
        {
            return Result<WorkTaskDto>.Failure(new Error(
                "WorkModel.Task.ObligationInvalid",
                $"Unknown obligation '{request.Obligation}'. Expected one of: mandatory, optional, conditional."));
        }

        EvidencePolicy? evidencePolicy = null;
        if (request.EvidencePolicy is not null)
        {
            if (!WorkModelWireValues.TryParseEvidencePolicy(request.EvidencePolicy, out var parsed))
            {
                return Result<WorkTaskDto>.Failure(new Error(
                    "WorkModel.Task.EvidencePolicyInvalid",
                    $"Unknown evidence policy '{request.EvidencePolicy}'."));
            }

            evidencePolicy = parsed;
        }

        EvidenceKind? evidenceKind = null;
        if (request.RequiredEvidenceKind is not null)
        {
            if (!WorkModelWireValues.TryParseEvidenceKind(request.RequiredEvidenceKind, out var parsed))
            {
                return Result<WorkTaskDto>.Failure(new Error(
                    "WorkModel.Task.EvidenceKindInvalid",
                    $"Unknown evidence kind '{request.RequiredEvidenceKind}'."));
            }

            evidenceKind = parsed;
        }

        var inputs = new List<TaskInputSpec>();

        foreach (var input in request.Inputs ?? Array.Empty<TaskInputRequest>())
        {
            if (!WorkModelWireValues.TryParseInputBasis(input.Basis, out var basis))
            {
                return Result<WorkTaskDto>.Failure(new Error(
                    "WorkModel.Task.InputBasisInvalid",
                    $"Unknown input basis '{input.Basis}'. Expected one of: per_unit, per_execution."));
            }

            if (!WorkModelWireValues.TryParseInputKind(input.Kind, out var kind))
            {
                return Result<WorkTaskDto>.Failure(new Error(
                    "WorkModel.Task.InputKindInvalid",
                    $"Unknown input kind '{input.Kind}'."));
            }

            inputs.Add(new TaskInputSpec(
                input.ItemId,
                input.Quantity,
                input.UomId,
                basis,
                kind,
                input.TolerancePct,
                input.IsBlocking,
                input.RequiresTraceability));
        }

        var version = await _dbContext.FindVersionAsync(request.VersionId, cancellationToken);
        if (version is null || version.ProcessId != request.ProcessId)
        {
            return Result<WorkTaskDto>.Failure(new Error(
                "WorkModel.Version.NotFound",
                $"Version '{request.VersionId}' was not found for process '{request.ProcessId}'."));
        }

        var added = version.AddTask(new WorkTaskSpec(
            request.Code,
            request.Name,
            request.ResponsibleRoleId,
            completion,
            request.CompletionSpec,
            request.EstimatedDurationSeconds,
            request.StandardDurationSeconds,
            request.ProgressWeight,
            obligation,
            request.IsMilestone,
            request.IsParallelizable,
            request.IsRepeatable,
            evidencePolicy,
            evidenceKind,
            request.MinEvidenceCount,
            request.RequiredCapability,
            request.RequiredAssetType,
            request.Instructions,
            request.DisplaySeq,
            inputs));

        if (added.IsFailure)
        {
            return Result<WorkTaskDto>.Failure(added.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<WorkTaskDto>.Success(added.Value.ToDto());
    }
}
