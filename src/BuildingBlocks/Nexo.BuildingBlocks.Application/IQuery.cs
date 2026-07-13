using MediatR;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.BuildingBlocks.Application;

/// <summary>A read-only request that returns a <typeparamref name="TResponse"/> on success.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
