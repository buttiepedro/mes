using FluentValidation;

namespace Nexo.MasterData.Application;

public sealed class CreateUomCommandValidator : AbstractValidator<CreateUomCommand>
{
    public CreateUomCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Symbol)
            .NotEmpty()
            .MaximumLength(16);

        RuleFor(x => x.Magnitude)
            .NotEmpty()
            .Must(magnitude => MasterDataWireValues.TryParseMagnitude(magnitude, out _))
            .WithMessage("Magnitude must be one of: mass, length, area, volume, time, count, energy.");

        RuleFor(x => x.FactorToBase)
            .GreaterThan(0m)
            .WithMessage("The conversion factor to the base unit must be greater than zero.");

        RuleFor(x => x.Decimals)
            .InclusiveBetween((short)0, (short)9);

        RuleFor(x => x.ExternalRef)
            .MaximumLength(128)
            .When(x => x.ExternalRef is not null);
    }
}
