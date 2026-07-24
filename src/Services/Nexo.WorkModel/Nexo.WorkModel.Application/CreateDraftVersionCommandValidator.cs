using FluentValidation;

namespace Nexo.WorkModel.Application;

public sealed class CreateDraftVersionCommandValidator : AbstractValidator<CreateDraftVersionCommand>
{
    public CreateDraftVersionCommandValidator()
    {
        RuleFor(x => x.ProcessId).NotEmpty();

        RuleFor(x => x.Bump)
            .Must(bump => WorkModelWireValues.TryParseVersionBump(bump, out _))
            .WithMessage("Bump must be one of: major, minor, patch.");

        RuleFor(x => x.ChangeReason).MaximumLength(512).When(x => x.ChangeReason is not null);
    }
}
