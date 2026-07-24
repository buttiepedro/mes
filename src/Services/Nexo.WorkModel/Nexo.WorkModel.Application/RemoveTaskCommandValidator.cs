using FluentValidation;

namespace Nexo.WorkModel.Application;

public sealed class RemoveTaskCommandValidator : AbstractValidator<RemoveTaskCommand>
{
    public RemoveTaskCommandValidator()
    {
        RuleFor(x => x.ProcessId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
    }
}
