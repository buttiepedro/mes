using FluentValidation;

namespace Nexo.WorkModel.Application;

public sealed class SetGraphCommandValidator : AbstractValidator<SetGraphCommand>
{
    public SetGraphCommandValidator()
    {
        RuleFor(x => x.ProcessId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.Edges).NotNull();

        RuleForEach(x => x.Edges).ChildRules(edge =>
        {
            edge.RuleFor(x => x.FromTask).NotEmpty();
            edge.RuleFor(x => x.ToTask).NotEmpty();

            edge.RuleFor(x => x.Kind)
                .Must(kind => WorkModelWireValues.TryParseDependencyType(kind, out _))
                .WithMessage("The dependency kind must be one of: FS, SS, FF.");

            // G5: negative lag is deferred to V1.
            edge.RuleFor(x => x.LagSeconds)
                .GreaterThanOrEqualTo(0)
                .WithMessage("The lag cannot be negative (G5).");
        });
    }
}
