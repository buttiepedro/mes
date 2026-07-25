using FluentValidation;

namespace Nexo.Execution.Application;

public sealed class SkipTaskCommandValidator : AbstractValidator<SkipTaskCommand>
{
    public SkipTaskCommandValidator()
    {
        RuleFor(x => x.TaskRunId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(512);
    }
}
