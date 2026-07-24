using FluentValidation;

namespace Nexo.MasterData.Application;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.LegalName)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.TaxId).MaximumLength(32).When(x => x.TaxId is not null);
        RuleFor(x => x.ExternalRef).MaximumLength(128).When(x => x.ExternalRef is not null);
    }
}
