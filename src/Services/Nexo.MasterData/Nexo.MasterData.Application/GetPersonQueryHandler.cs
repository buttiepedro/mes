using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Application;

public sealed class GetPersonQueryHandler : IQueryHandler<GetPersonQuery, PersonDto>
{
    private readonly IMasterDataDbContext _dbContext;

    public GetPersonQueryHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<PersonDto>> Handle(GetPersonQuery request, CancellationToken cancellationToken)
    {
        var person = await _dbContext.FindPersonAsync(request.PersonId, cancellationToken);

        return person is null
            ? Result<PersonDto>.Failure(new Error(
                "MasterData.Person.NotFound",
                $"Person '{request.PersonId}' was not found."))
            : Result<PersonDto>.Success(person.ToDto());
    }
}
