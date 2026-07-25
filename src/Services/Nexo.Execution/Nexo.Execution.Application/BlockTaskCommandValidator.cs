using FluentValidation;

namespace Nexo.Execution.Application;

public sealed class BlockTaskCommandValidator : AbstractValidator<BlockTaskCommand>
{
    public BlockTaskCommandValidator()
    {
        RuleFor(x => x.TaskRunId).NotEmpty();

        RuleFor(x => x.Cause)
            .Must(cause => ExecutionWireValues.TryParseBlockCause(cause, out _))
            .WithMessage("Block cause must be one of: input, resource, approval, quality.");
    }
}
