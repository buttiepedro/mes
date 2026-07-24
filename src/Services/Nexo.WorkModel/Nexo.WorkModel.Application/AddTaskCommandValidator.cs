using FluentValidation;

namespace Nexo.WorkModel.Application;

public sealed class AddTaskCommandValidator : AbstractValidator<AddTaskCommand>
{
    public AddTaskCommandValidator()
    {
        RuleFor(x => x.ProcessId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        // W3: role first, nominated person is the exception.
        RuleFor(x => x.ResponsibleRoleId)
            .NotEmpty()
            .WithMessage("Every task must declare a responsible role (W3).");

        RuleFor(x => x.CompletionKind)
            .Must(kind => WorkModelWireValues.TryParseCompletionKind(kind, out _))
            .WithMessage("Completion kind must be one of: declarative, quantity, measurement, signal, evidence, quality, approval, composite.");

        RuleFor(x => x.Obligation)
            .Must(obligation => WorkModelWireValues.TryParseObligation(obligation, out _))
            .WithMessage("Obligation must be one of: mandatory, optional, conditional.");

        RuleFor(x => x.StandardDurationSeconds)
            .GreaterThan(0m)
            .When(x => x.StandardDurationSeconds.HasValue)
            .WithMessage("The standard duration must be greater than zero when supplied (W7).");

        RuleFor(x => x.EstimatedDurationSeconds)
            .GreaterThan(0m)
            .When(x => x.EstimatedDurationSeconds.HasValue)
            .WithMessage("The estimated duration must be greater than zero when supplied.");

        RuleFor(x => x.ProgressWeight)
            .InclusiveBetween(0m, 100m)
            .When(x => x.ProgressWeight.HasValue)
            .WithMessage("The progress weight must be between 0 and 100.");

        RuleFor(x => x.MinEvidenceCount)
            .GreaterThanOrEqualTo((short)0);

        RuleForEach(x => x.Inputs).ChildRules(input =>
        {
            input.RuleFor(x => x.ItemId).NotEmpty();
            input.RuleFor(x => x.UomId).NotEmpty();
            input.RuleFor(x => x.Quantity).GreaterThan(0m);
            input.RuleFor(x => x.Basis)
                .Must(basis => WorkModelWireValues.TryParseInputBasis(basis, out _))
                .WithMessage("Input basis must be one of: per_unit, per_execution.");
            input.RuleFor(x => x.Kind)
                .Must(kind => WorkModelWireValues.TryParseInputKind(kind, out _))
                .WithMessage("Input kind must be one of: material, component, tool, service, external_labor.");
        });
    }
}
