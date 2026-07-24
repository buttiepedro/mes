using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>Returns a single customer by id.</summary>
public sealed record GetCustomerQuery(Guid CustomerId) : IQuery<CustomerDto>;
