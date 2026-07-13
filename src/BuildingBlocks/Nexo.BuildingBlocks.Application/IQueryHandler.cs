using MediatR;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.BuildingBlocks.Application;

/// <summary>Handles a query that returns a <typeparamref name="TResponse"/> on success.</summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
