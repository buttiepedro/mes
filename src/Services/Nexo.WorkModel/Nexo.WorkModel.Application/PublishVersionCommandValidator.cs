using FluentValidation;

namespace Nexo.WorkModel.Application;

public sealed class PublishVersionCommandValidator : AbstractValidator<PublishVersionCommand>
{
    public PublishVersionCommandValidator()
    {
        RuleFor(x => x.ProcessId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
    }
}
