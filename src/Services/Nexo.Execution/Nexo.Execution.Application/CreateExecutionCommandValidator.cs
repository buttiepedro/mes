using FluentValidation;

namespace Nexo.Execution.Application;

public sealed class CreateExecutionCommandValidator : AbstractValidator<CreateExecutionCommand>
{
    public CreateExecutionCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Snapshot)
            .NotNull();

        RuleFor(x => x.Snapshot.Profile)
            .Must(profile => ExecutionWireValues.TryParseFlavorFromProfile(profile, out _))
            .WithMessage("Profile must be one of: repetitive, project.");

        RuleFor(x => x.Snapshot.Tasks)
            .NotEmpty()
            .WithMessage("The snapshot must carry at least one task to instantiate.");

        RuleForEach(x => x.Snapshot.Tasks).ChildRules(task =>
        {
            task.RuleFor(t => t.TaskId).NotEmpty();
            task.RuleFor(t => t.Code).NotEmpty().MaximumLength(64);
            task.RuleFor(t => t.Name).NotEmpty().MaximumLength(256);
            task.RuleFor(t => t.ResponsibleRoleId).NotEmpty();
            task.RuleFor(t => t.MinEvidenceCount).GreaterThanOrEqualTo((short)0);
        });

        RuleFor(x => x.Trigger.Type)
            .Must(type => ExecutionWireValues.TryParseTriggerKind(type, out _))
            .WithMessage("Trigger type must be one of: work_order, plan, stock, rule, contract, quote, maintenance, manual.");

        // E4: a batch target, when supplied, needs a product and a positive quantity.
        When(x => x.Target is not null, () =>
        {
            RuleFor(x => x.Target!.ItemId).NotEmpty();
            RuleFor(x => x.Target!.UomId).NotEmpty();
            RuleFor(x => x.Target!.Quantity).GreaterThan(0m);
        });

        // E5: a project commitment, when supplied, needs a deliverable and a committed date.
        When(x => x.Commitment is not null, () =>
        {
            RuleFor(x => x.Commitment!.Deliverable).NotEmpty().MaximumLength(512);
            RuleFor(x => x.Commitment!.CommittedDate).NotEqual(default(DateTimeOffset));
        });
    }
}
