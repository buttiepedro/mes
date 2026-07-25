using FluentValidation;

namespace Nexo.Execution.Application;

public sealed class ReportProgressCommandValidator : AbstractValidator<ReportProgressCommand>
{
    public ReportProgressCommandValidator()
    {
        RuleFor(x => x.TaskRunId).NotEmpty();

        RuleFor(x => x.Method)
            .Must(method => ExecutionWireValues.TryParseProgressMethod(method, out _))
            .WithMessage("Progress method must be one of: declared, quantity, checklist, time, signal.");

        RuleFor(x => x.ProgressPct)
            .InclusiveBetween(0m, 100m);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.Quantity.HasValue);
    }
}
