using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class AttachEvidenceCommandHandler : ICommandHandler<AttachEvidenceCommand, EvidenceDto>
{
    private readonly IExecutionDbContext _dbContext;

    public AttachEvidenceCommandHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<EvidenceDto>> Handle(AttachEvidenceCommand request, CancellationToken cancellationToken)
    {
        if (!ExecutionWireValues.TryParseEvidenceKind(request.Kind, out var kind))
        {
            return Result<EvidenceDto>.Failure(new Error(
                "Execution.EvidenceKindInvalid",
                $"Unknown evidence kind '{request.Kind}'. Expected one of: photo, file, sensor_reading, signature, video, form."));
        }

        if (!ExecutionWireValues.TryParseEvidenceStatus(request.Status, out var status))
        {
            return Result<EvidenceDto>.Failure(new Error(
                "Execution.EvidenceStatusInvalid",
                $"Unknown evidence status '{request.Status}'. Expected one of: pending, materialized, verified."));
        }

        byte[]? contentHash = null;
        if (!string.IsNullOrWhiteSpace(request.ContentHash))
        {
            try
            {
                contentHash = Convert.FromHexString(request.ContentHash.Trim());
            }
            catch (FormatException)
            {
                return Result<EvidenceDto>.Failure(new Error(
                    "Execution.EvidenceHashInvalid",
                    "The content hash must be a hexadecimal string."));
            }
        }

        var execution = await _dbContext.FindExecutionByTaskRunAsync(request.TaskRunId, cancellationToken);
        if (execution is null)
        {
            return Result<EvidenceDto>.Failure(ExecutionErrors.TaskRunNotFound(request.TaskRunId.ToString()));
        }

        var attached = execution.AttachEvidence(
            request.TaskRunId,
            kind,
            status,
            request.FileId,
            request.MediaRef,
            contentHash,
            request.RequirementId,
            request.CapturedBy,
            request.Caption);

        if (attached.IsFailure)
        {
            return Result<EvidenceDto>.Failure(attached.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<EvidenceDto>.Success(attached.Value.ToDto());
    }
}
