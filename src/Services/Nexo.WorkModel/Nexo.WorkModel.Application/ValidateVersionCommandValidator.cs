using FluentValidation;

namespace Nexo.WorkModel.Application;

public sealed class ValidateVersionCommandValidator : AbstractValidator<ValidateVersionCommand>
{
    public ValidateVersionCommandValidator()
    {
        RuleFor(x => x.ProcessId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
    }
}
