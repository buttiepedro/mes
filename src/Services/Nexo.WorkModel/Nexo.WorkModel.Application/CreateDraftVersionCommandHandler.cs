using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Application;

public sealed class CreateDraftVersionCommandHandler : ICommandHandler<CreateDraftVersionCommand, ProcessVersionDto>
{
    private readonly IWorkModelDbContext _dbContext;

    public CreateDraftVersionCommandHandler(IWorkModelDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ProcessVersionDto>> Handle(CreateDraftVersionCommand request, CancellationToken cancellationToken)
    {
        if (!WorkModelWireValues.TryParseVersionBump(request.Bump, out var bump))
        {
            return Result<ProcessVersionDto>.Failure(new Error(
                "WorkModel.Version.BumpInvalid",
                $"Unknown version bump '{request.Bump}'. Expected one of: major, minor, patch."));
        }

        var process = await _dbContext.FindProcessAsync(request.ProcessId, cancellationToken);
        if (process is null)
        {
            return Result<ProcessVersionDto>.Failure(new Error(
                "WorkModel.Process.NotFound",
                $"Process '{request.ProcessId}' was not found."));
        }

        // The published version is the natural parent of a new draft; when there is none (everything is
        // suspended or the library is still being written) the newest version is used instead.
        var source = await _dbContext.FindPublishedVersionAsync(process.Id, cancellationToken)
            ?? await _dbContext.FindLatestVersionAsync(process.Id, cancellationToken);

        var derived = source is null
            ? process.StartInitialVersion(request.ChangeReason)
            : process.DeriveVersion(source, bump, request.ChangeReason);

        if (derived.IsFailure)
        {
            return Result<ProcessVersionDto>.Failure(derived.Error);
        }

        var draft = derived.Value;

        if (await _dbContext.VersionNumberExistsAsync(process.Id, draft.VersionNo, cancellationToken))
        {
            return Result<ProcessVersionDto>.Failure(new Error(
                "WorkModel.Version.NumberConflict",
                $"Version '{draft.VersionNo}' already exists for process '{process.Code}'."));
        }

        _dbContext.AddVersion(draft);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ProcessVersionDto>.Success(draft.ToDto());
    }
}
