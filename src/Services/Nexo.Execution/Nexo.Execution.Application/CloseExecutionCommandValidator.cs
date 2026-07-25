using FluentValidation;

namespace Nexo.Execution.Application;

public sealed class CloseExecutionCommandValidator : AbstractValidator<CloseExecutionCommand>
{
    public CloseExecutionCommandValidator()
    {
        RuleFor(x => x.ExecutionId).NotEmpty();

        RuleFor(x => x.Mode)
            .Must(mode => ExecutionWireValues.TryParseCloseKind(mode, out _))
            .WithMessage("Close mode must be one of: normal, partial, forced, cancelled, expired.");
    }
}
