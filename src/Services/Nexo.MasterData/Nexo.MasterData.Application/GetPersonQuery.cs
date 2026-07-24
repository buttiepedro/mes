using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>Returns a single operational person by id.</summary>
public sealed record GetPersonQuery(Guid PersonId) : IQuery<PersonDto>;
