using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Application;

public sealed class ValidateVersionCommandHandler : ICommandHandler<ValidateVersionCommand, VersionValidationDto>
{
    private readonly IWorkModelDbContext _dbContext;

    public ValidateVersionCommandHandler(IWorkModelDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<VersionValidationDto>> Handle(ValidateVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _dbContext.FindVersionAsync(request.VersionId, cancellationToken);
        if (version is null || version.ProcessId != request.ProcessId)
        {
            return Result<VersionValidationDto>.Failure(new Error(
                "WorkModel.Version.NotFound",
                $"Version '{request.VersionId}' was not found for process '{request.ProcessId}'."));
        }

        return Result<VersionValidationDto>.Success(version.Validate().ToValidationDto());
    }
}
