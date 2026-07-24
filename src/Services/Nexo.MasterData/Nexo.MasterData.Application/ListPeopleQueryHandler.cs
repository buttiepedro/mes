using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Application;

public sealed class ListPeopleQueryHandler : IQueryHandler<ListPeopleQuery, IReadOnlyList<PersonDto>>
{
    private readonly IMasterDataDbContext _dbContext;

    public ListPeopleQueryHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<IReadOnlyList<PersonDto>>> Handle(ListPeopleQuery request, CancellationToken cancellationToken)
    {
        MasterStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!MasterDataWireValues.TryParseStatus(request.Status, out var parsedStatus))
            {
                return Result<IReadOnlyList<PersonDto>>.Failure(new Error(
                    "MasterData.Status.Invalid",
                    $"Unknown status '{request.Status}'. Expected one of: active, archived."));
            }

            status = parsedStatus;
        }

        var people = await _dbContext.ListPeopleAsync(
            status,
            request.Search,
            PagingDefaults.Clamp(request.Limit),
            PagingDefaults.NormalizeOffset(request.Offset),
            cancellationToken);

        return Result<IReadOnlyList<PersonDto>>.Success(people.Select(person => person.ToDto()).ToArray());
    }
}
