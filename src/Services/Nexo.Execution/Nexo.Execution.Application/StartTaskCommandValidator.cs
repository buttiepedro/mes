using FluentValidation;

namespace Nexo.Execution.Application;

public sealed class StartTaskCommandValidator : AbstractValidator<StartTaskCommand>
{
    public StartTaskCommandValidator()
    {
        RuleFor(x => x.TaskRunId).NotEmpty();
    }
}
