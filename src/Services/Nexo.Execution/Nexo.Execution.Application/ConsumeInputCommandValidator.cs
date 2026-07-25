using FluentValidation;

namespace Nexo.Execution.Application;

public sealed class ConsumeInputCommandValidator : AbstractValidator<ConsumeInputCommand>
{
    public ConsumeInputCommandValidator()
    {
        RuleFor(x => x.ExecutionId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.UomId).NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0m)
            .WithMessage("A real consumption must declare a quantity greater than zero.");

        RuleFor(x => x.Method)
            .Must(method => ExecutionWireValues.TryParseConsumptionMethod(method, out _))
            .WithMessage("Consumption method must be one of: declared, backflush, scale, scan, adjustment.");
    }
}
