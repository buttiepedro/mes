using FluentValidation;

namespace Nexo.Execution.Application;

public sealed class CancelExecutionCommandValidator : AbstractValidator<CancelExecutionCommand>
{
    public CancelExecutionCommandValidator()
    {
        RuleFor(x => x.ExecutionId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(512);
    }
}
