using FluentValidation;

namespace Nexo.Production.Application;

public sealed class RegisterProductionCommandValidator : AbstractValidator<RegisterProductionCommand>
{
    private static readonly string[] AllowedSources = { "Manual", "Datalogger" };

    public RegisterProductionCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();

        RuleFor(x => x.GoodQty)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Good quantity must be >= 0.");

        RuleFor(x => x.ScrapQty)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Scrap quantity must be >= 0.");

        RuleFor(x => x.OperatorId).NotEmpty();

        RuleFor(x => x.Source)
            .NotEmpty()
            .Must(source => AllowedSources.Contains(source, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Source must be one of: Manual, Datalogger.");
    }
}
