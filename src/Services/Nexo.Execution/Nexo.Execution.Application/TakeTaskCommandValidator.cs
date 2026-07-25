using FluentValidation;

namespace Nexo.Execution.Application;

public sealed class TakeTaskCommandValidator : AbstractValidator<TakeTaskCommand>
{
    public TakeTaskCommandValidator()
    {
        RuleFor(x => x.TaskRunId).NotEmpty();

        RuleFor(x => x.Mode)
            .Must(mode => ExecutionWireValues.TryParseAssignmentMode(mode, out _))
            .WithMessage("Assignment mode must be one of: individual, crew, role_open, automatic, external.");
    }
}
