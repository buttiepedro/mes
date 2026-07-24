using FluentValidation;

namespace Nexo.WorkModel.Application;

public sealed class CreateProcessCommandValidator : AbstractValidator<CreateProcessCommand>
{
    public CreateProcessCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Profile)
            .Must(profile => WorkModelWireValues.TryParseProfile(profile, out _))
            .WithMessage("Profile must be one of: repetitive, project.");

        RuleFor(x => x.EvidencePolicy)
            .Must(policy => WorkModelWireValues.TryParseEvidencePolicy(policy, out _))
            .WithMessage("Evidence policy must be one of: mandatory, recommended, optional, none.");

        RuleFor(x => x.SkipPolicy)
            .Must(policy => WorkModelWireValues.TryParseSkipPolicy(policy, out _))
            .WithMessage("Skip policy must be one of: allowed, authorized, forbidden.");

        RuleFor(x => x.ExternalRef).MaximumLength(128).When(x => x.ExternalRef is not null);
    }
}
