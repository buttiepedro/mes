using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Application;

public sealed class CreateProcessCommandHandler : ICommandHandler<CreateProcessCommand, ProcessCreatedDto>
{
    private readonly IWorkModelDbContext _dbContext;

    public CreateProcessCommandHandler(IWorkModelDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ProcessCreatedDto>> Handle(CreateProcessCommand request, CancellationToken cancellationToken)
    {
        if (!WorkModelWireValues.TryParseProfile(request.Profile, out var profile))
        {
            return Result<ProcessCreatedDto>.Failure(new Error(
                "WorkModel.Process.ProfileInvalid",
                $"Unknown process profile '{request.Profile}'. Expected one of: repetitive, project."));
        }

        if (!WorkModelWireValues.TryParseEvidencePolicy(request.EvidencePolicy, out var evidencePolicy))
        {
            return Result<ProcessCreatedDto>.Failure(new Error(
                "WorkModel.Process.EvidencePolicyInvalid",
                $"Unknown evidence policy '{request.EvidencePolicy}'. Expected one of: mandatory, recommended, optional, none."));
        }

        if (!WorkModelWireValues.TryParseSkipPolicy(request.SkipPolicy, out var skipPolicy))
        {
            return Result<ProcessCreatedDto>.Failure(new Error(
                "WorkModel.Process.SkipPolicyInvalid",
                $"Unknown skip policy '{request.SkipPolicy}'. Expected one of: allowed, authorized, forbidden."));
        }

        // W13: the process code is the natural key of the tenant's library.
        if (await _dbContext.ProcessCodeExistsAsync(request.Code, cancellationToken))
        {
            return Result<ProcessCreatedDto>.Failure(new Error(
                "WorkModel.Process.CodeConflict",
                $"A process with code '{request.Code}' already exists in this tenant (W13)."));
        }

        var process = Process.Create(
            request.Code,
            request.Name,
            profile,
            request.OutputItemId,
            request.OutputUomId,
            request.SiteId,
            request.AreaId,
            request.LineId,
            evidencePolicy,
            skipPolicy,
            request.Tags,
            request.ExternalRef);

        var version = process.StartInitialVersion();
        if (version.IsFailure)
        {
            return Result<ProcessCreatedDto>.Failure(version.Error);
        }

        _dbContext.AddProcess(process);
        _dbContext.AddVersion(version.Value);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ProcessCreatedDto>.Success(
            new ProcessCreatedDto(process.Id, version.Value.Id, version.Value.VersionNo));
    }
}
