using FluentValidation;

namespace Nexo.Execution.Application;

public sealed class CompleteTaskCommandValidator : AbstractValidator<CompleteTaskCommand>
{
    public CompleteTaskCommandValidator()
    {
        RuleFor(x => x.TaskRunId).NotEmpty();

        // A forced close must justify itself (E19).
        RuleFor(x => x.Reason)
            .NotEmpty()
            .When(x => x.Force)
            .WithMessage("A forced completion requires a reason (E19).");
    }
}
