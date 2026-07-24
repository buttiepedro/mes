using FluentValidation;

namespace Nexo.WorkModel.Application;

public sealed class SuspendVersionCommandValidator : AbstractValidator<SuspendVersionCommand>
{
    public SuspendVersionCommandValidator()
    {
        RuleFor(x => x.ProcessId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512).When(x => x.Reason is not null);
    }
}
