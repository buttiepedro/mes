using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class CreateExecutionCommandHandler : ICommandHandler<CreateExecutionCommand, ExecutionCreatedDto>
{
    private readonly IExecutionDbContext _dbContext;

    public CreateExecutionCommandHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ExecutionCreatedDto>> Handle(CreateExecutionCommand request, CancellationToken cancellationToken)
    {
        // E3: the flavour derives from the process profile, never from the trigger.
        if (!ExecutionWireValues.TryParseFlavorFromProfile(request.Snapshot.Profile, out var flavor))
        {
            return Result<ExecutionCreatedDto>.Failure(new Error(
                "Execution.ProfileInvalid",
                $"Unknown process profile '{request.Snapshot.Profile}'. Expected one of: repetitive, project."));
        }

        if (!ExecutionWireValues.TryParseTriggerKind(request.Trigger.Type, out var triggerKind))
        {
            return Result<ExecutionCreatedDto>.Failure(new Error(
                "Execution.TriggerKindInvalid",
                $"Unknown trigger kind '{request.Trigger.Type}'."));
        }

        var tasks = new List<TaskSnapshot>(request.Snapshot.Tasks.Count);
        foreach (var task in request.Snapshot.Tasks)
        {
            if (!ExecutionWireValues.TryParseObligation(task.Obligation, out var obligation))
            {
                return Result<ExecutionCreatedDto>.Failure(new Error(
                    "Execution.ObligationInvalid",
                    $"Unknown obligation '{task.Obligation}' for task '{task.Code}'. Expected one of: mandatory, optional, conditional."));
            }

            EvidenceKind? requiredEvidenceKind = null;
            if (task.RequiredEvidenceKind is not null)
            {
                if (!ExecutionWireValues.TryParseEvidenceKind(task.RequiredEvidenceKind, out var parsedKind))
                {
                    return Result<ExecutionCreatedDto>.Failure(new Error(
                        "Execution.EvidenceKindInvalid",
                        $"Unknown evidence kind '{task.RequiredEvidenceKind}' for task '{task.Code}'."));
                }

                requiredEvidenceKind = parsedKind;
            }

            tasks.Add(new TaskSnapshot(
                task.TaskId,
                task.Code,
                task.Name,
                task.ResponsibleRoleId,
                task.SuggestedPersonId,
                task.StandardDurationSeconds,
                task.EstimatedDurationSeconds,
                task.ProgressWeight,
                obligation,
                task.IsMilestone,
                requiredEvidenceKind,
                task.MinEvidenceCount));
        }

        var precedences = new List<PrecedenceSnapshot>(request.Snapshot.Precedences.Count);
        foreach (var edge in request.Snapshot.Precedences)
        {
            if (!ExecutionWireValues.TryParseDependencyType(edge.Type, out var type))
            {
                return Result<ExecutionCreatedDto>.Failure(new Error(
                    "Execution.DependencyTypeInvalid",
                    $"Unknown precedence type '{edge.Type}'. Expected one of: FS, SS, FF."));
            }

            precedences.Add(new PrecedenceSnapshot(edge.PredecessorTaskId, edge.SuccessorTaskId, type, edge.LagSeconds));
        }

        // The execution code is the natural key of the run within the tenant.
        if (await _dbContext.ExecutionCodeExistsAsync(request.Code, cancellationToken))
        {
            return Result<ExecutionCreatedDto>.Failure(new Error(
                "Execution.CodeConflict",
                $"An execution with code '{request.Code}' already exists in this tenant."));
        }

        var snapshot = new ProcessSnapshot(
            request.Snapshot.ProcessId,
            request.Snapshot.ProcessVersionId,
            request.Snapshot.VersionNo,
            flavor,
            tasks,
            precedences);

        var trigger = new ExecutionTrigger(
            triggerKind,
            request.Trigger.RefKind,
            request.Trigger.RefId,
            request.Trigger.ExternalRef);

        var target = request.Target is null
            ? null
            : new BatchTarget(request.Target.ItemId, request.Target.Quantity, request.Target.UomId);

        var commitment = request.Commitment is null
            ? null
            : new ProjectCommitment(
                request.Commitment.Deliverable,
                request.Commitment.CommittedDate,
                request.Commitment.CustomerId,
                request.Commitment.DeliverableItemId,
                request.Commitment.ContractRef);

        var scope = new ExecutionScope(request.SiteId, request.AreaId, request.LineId, request.WorkCenterId);

        var created = Domain.Execution.Create(
            request.Code,
            snapshot,
            trigger,
            target,
            commitment,
            scope,
            request.OwnerPersonId,
            request.Priority);

        if (created.IsFailure)
        {
            return Result<ExecutionCreatedDto>.Failure(created.Error);
        }

        var execution = created.Value;
        _dbContext.AddExecution(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ExecutionCreatedDto>.Success(new ExecutionCreatedDto(
            execution.Id,
            execution.Code,
            execution.Flavor.ToWireValue(),
            execution.Status.ToWireValue(),
            execution.TaskRuns.Count));
    }
}
