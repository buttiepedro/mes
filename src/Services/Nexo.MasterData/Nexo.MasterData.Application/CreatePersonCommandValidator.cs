using FluentValidation;

namespace Nexo.MasterData.Application;

public sealed class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(256);

        // A person may exist WITHOUT a user account: an operator who clocks in with a badge does not
        // need one. When supplied, it must be a real identifier.
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .When(x => x.UserId.HasValue);

        RuleFor(x => x.ExternalRef).MaximumLength(128).When(x => x.ExternalRef is not null);
    }
}
