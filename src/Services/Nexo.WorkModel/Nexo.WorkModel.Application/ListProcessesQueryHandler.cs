using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Application;

public sealed class ListProcessesQueryHandler : IQueryHandler<ListProcessesQuery, IReadOnlyList<ProcessDto>>
{
    private readonly IWorkModelDbContext _dbContext;

    public ListProcessesQueryHandler(IWorkModelDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<IReadOnlyList<ProcessDto>>> Handle(ListProcessesQuery request, CancellationToken cancellationToken)
    {
        ProcessProfile? profile = null;
        if (!string.IsNullOrWhiteSpace(request.Profile))
        {
            if (!WorkModelWireValues.TryParseProfile(request.Profile, out var parsed))
            {
                return Result<IReadOnlyList<ProcessDto>>.Failure(new Error(
                    "WorkModel.Process.ProfileInvalid",
                    $"Unknown process profile '{request.Profile}'. Expected one of: repetitive, project."));
            }

            profile = parsed;
        }

        ProcessStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!WorkModelWireValues.TryParseProcessStatus(request.Status, out var parsed))
            {
                return Result<IReadOnlyList<ProcessDto>>.Failure(new Error(
                    "WorkModel.Status.Invalid",
                    $"Unknown status '{request.Status}'. Expected one of: active, archived."));
            }

            status = parsed;
        }

        var processes = await _dbContext.ListProcessesAsync(
            profile,
            status,
            request.Search,
            PagingDefaults.Clamp(request.Limit),
            PagingDefaults.NormalizeOffset(request.Offset),
            cancellationToken);

        return Result<IReadOnlyList<ProcessDto>>.Success(processes.Select(process => process.ToDto()).ToArray());
    }
}
