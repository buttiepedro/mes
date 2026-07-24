using FluentValidation;

namespace Nexo.MasterData.Application;

public sealed class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Roles)
            .NotNull()
            .Must(roles => roles is { Count: > 0 })
            .WithMessage("An item must declare at least one role.")
            .Must(roles => MasterDataWireValues.ParseRoles(roles) is not null)
            .WithMessage("Roles must be one of: product, input.");

        RuleFor(x => x.Tracking)
            .Must(tracking => MasterDataWireValues.TryParseTracking(tracking, out _))
            .WithMessage("Tracking must be one of: none, batch, serial.");

        RuleFor(x => x.IdealCycleTime)
            .GreaterThan(0m)
            .When(x => x.IdealCycleTime.HasValue)
            .WithMessage("The ideal cycle time must be greater than zero when supplied.");

        RuleFor(x => x.Category).MaximumLength(64).When(x => x.Category is not null);
        RuleFor(x => x.Family).MaximumLength(128).When(x => x.Family is not null);
    }
}
