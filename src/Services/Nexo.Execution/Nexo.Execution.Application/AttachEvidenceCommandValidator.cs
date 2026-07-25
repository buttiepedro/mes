using FluentValidation;

namespace Nexo.Execution.Application;

public sealed class AttachEvidenceCommandValidator : AbstractValidator<AttachEvidenceCommand>
{
    public AttachEvidenceCommandValidator()
    {
        RuleFor(x => x.TaskRunId).NotEmpty();

        RuleFor(x => x.Kind)
            .Must(kind => ExecutionWireValues.TryParseEvidenceKind(kind, out _))
            .WithMessage("Evidence kind must be one of: photo, file, sensor_reading, signature, video, form.");

        RuleFor(x => x.Status)
            .Must(status => ExecutionWireValues.TryParseEvidenceStatus(status, out _))
            .WithMessage("Evidence status must be one of: pending, materialized, verified.");

        // A piece of evidence must reference its content in some form.
        RuleFor(x => x)
            .Must(x => x.FileId is not null || !string.IsNullOrWhiteSpace(x.MediaRef))
            .WithMessage("Evidence must carry a file reference or a media reference.");
    }
}
