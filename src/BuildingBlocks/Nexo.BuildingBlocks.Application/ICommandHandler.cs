using MediatR;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.BuildingBlocks.Application;

/// <summary>Handles a command that returns no value beyond success or failure.</summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>Handles a command that returns a <typeparamref name="TResponse"/> on success.</summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
