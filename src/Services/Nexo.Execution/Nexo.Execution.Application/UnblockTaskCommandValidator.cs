using FluentValidation;

namespace Nexo.Execution.Application;

public sealed class UnblockTaskCommandValidator : AbstractValidator<UnblockTaskCommand>
{
    public UnblockTaskCommandValidator()
    {
        RuleFor(x => x.TaskRunId).NotEmpty();
    }
}
