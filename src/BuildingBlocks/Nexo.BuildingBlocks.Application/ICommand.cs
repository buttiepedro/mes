using MediatR;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.BuildingBlocks.Application;

/// <summary>A command that returns no value beyond success or failure.</summary>
public interface ICommand : IRequest<Result> { }

/// <summary>A command that returns a <typeparamref name="TResponse"/> on success.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
