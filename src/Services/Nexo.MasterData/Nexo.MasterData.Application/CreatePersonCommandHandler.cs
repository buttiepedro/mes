using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Application;

public sealed class CreatePersonCommandHandler : ICommandHandler<CreatePersonCommand, Guid>
{
    private readonly IMasterDataDbContext _dbContext;

    public CreatePersonCommandHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<Guid>> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        if (await _dbContext.PersonCodeExistsAsync(request.Code, cancellationToken))
        {
            return Result<Guid>.Failure(new Error(
                "MasterData.Person.CodeConflict",
                $"A person with code '{request.Code}' already exists in this tenant."));
        }

        var person = Person.Create(
            request.Code,
            request.FullName,
            request.DefaultRoleId,
            request.SiteId,
            request.LineId,
            request.UserId,
            request.Calendar,
            MasterGovernance.Local,
            request.ExternalRef);

        _dbContext.AddPerson(person);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(person.Id);
    }
}
