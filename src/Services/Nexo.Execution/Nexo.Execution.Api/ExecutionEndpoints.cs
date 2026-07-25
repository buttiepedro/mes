using MediatR;
using Nexo.Execution.Application;

namespace Nexo.Execution.Api;

/// <summary>
/// Minimal API endpoints for the Execution slice under <c>/v1</c>
/// (docs/design/04-service-contracts.md §2.7). Reads require <c>nexo.execution.read</c> and every
/// operation (create, task transitions, consumption, evidence, close/cancel) requires
/// <c>nexo.execution.write</c>.
/// </summary>
public static class ExecutionEndpoints
{
    public static IEndpointRouteBuilder MapExecutionEndpoints(this IEndpointRouteBuilder app)
    {
        MapExecutionRoutes(app);
        MapTaskRoutes(app);

        return app;
    }

    // --- Executions ---------------------------------------------------------------------------

    private static void MapExecutionRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/executions").WithTags("Execution · Runs");

        group.MapGet("/", ListExecutionsAsync)
            .WithName("ListExecutions")
            .RequireAuthorization("execution.read");

        // The imputation backlog (E24): task runs whose work is not yet imputed to a person.
        group.MapGet("/pending-imputation", ListPendingImputationAsync)
            .WithName("ListPendingImputation")
            .RequireAuthorization("execution.read");

        // The run with its task runs and materialized progress — the frozen version made observable.
        group.MapGet("/{executionId:guid}", GetExecutionSnapshotAsync)
            .WithName("GetExecutionSnapshot")
            .RequireAuthorization("execution.read");

        group.MapPost("/", CreateExecutionAsync)
            .WithName("CreateExecution")
            .RequireAuthorization("execution.write");

        group.MapPost("/{executionId:guid}/inputs", ConsumeInputAsync)
            .WithName("ConsumeInput")
            .RequireAuthorization("execution.write");

        group.MapPost("/{executionId:guid}:close", CloseExecutionAsync)
            .WithName("CloseExecution")
            .RequireAuthorization("execution.write");

        group.MapPost("/{executionId:guid}:cancel", CancelExecutionAsync)
            .WithName("CancelExecution")
            .RequireAuthorization("execution.write");
    }

    private static async Task<IResult> ListExecutionsAsync(
        ISender sender,
        CancellationToken cancellationToken,
        string? flavor = null,
        string? status = null,
        Guid? process_id = null,
        DateTimeOffset? due_before = null,
        int limit = PagingDefaults.DefaultLimit,
        int offset = 0)
    {
        var result = await sender.Send(
            new ListExecutionsQuery(flavor, status, process_id, due_before, limit, offset),
            cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> ListPendingImputationAsync(
        ISender sender,
        CancellationToken cancellationToken,
        int limit = PagingDefaults.DefaultLimit,
        int offset = 0)
    {
        var result = await sender.Send(new ListPendingImputationQuery(limit, offset), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> GetExecutionSnapshotAsync(
        Guid executionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExecutionSnapshotQuery(executionId), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> CreateExecutionAsync(
        CreateExecutionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateExecutionCommand(
                request.Code,
                request.Snapshot,
                request.Trigger,
                request.Target,
                request.Commitment,
                request.OwnerPersonId,
                request.Priority,
                request.SiteId,
                request.AreaId,
                request.LineId,
                request.WorkCenterId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/v1/executions/{result.Value.ExecutionId}", result.Value)
            : result.ToProblem();
    }

    private static async Task<IResult> ConsumeInputAsync(
        Guid executionId,
        ConsumeInputRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ConsumeInputCommand(
                executionId,
                request.ItemId,
                request.Quantity,
                request.UomId,
                request.Method,
                request.TaskRunId,
                request.TaskInputId,
                request.PlannedQuantity,
                request.BatchId,
                request.SerialId,
                request.PersonId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/v1/executions/{executionId}/inputs/{result.Value.Id}", result.Value)
            : result.ToProblem();
    }

    private static async Task<IResult> CloseExecutionAsync(
        Guid executionId,
        CloseExecutionRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CloseExecutionCommand(executionId, request?.Mode ?? "normal", request?.Reason),
            cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> CancelExecutionAsync(
        Guid executionId,
        CancelExecutionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelExecutionCommand(executionId, request.Reason), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    // --- Task runs ----------------------------------------------------------------------------

    private static void MapTaskRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/tasks").WithTags("Execution · Task runs");

        group.MapPost("/{taskRunId:guid}:take", TakeTaskAsync)
            .WithName("TakeTask")
            .RequireAuthorization("execution.write");

        group.MapPost("/{taskRunId:guid}:start", StartTaskAsync)
            .WithName("StartTask")
            .RequireAuthorization("execution.write");

        group.MapPost("/{taskRunId:guid}:progress", ReportProgressAsync)
            .WithName("ReportProgress")
            .RequireAuthorization("execution.write");

        group.MapPost("/{taskRunId:guid}:block", BlockTaskAsync)
            .WithName("BlockTask")
            .RequireAuthorization("execution.write");

        group.MapPost("/{taskRunId:guid}:unblock", UnblockTaskAsync)
            .WithName("UnblockTask")
            .RequireAuthorization("execution.write");

        group.MapPost("/{taskRunId:guid}:complete", CompleteTaskAsync)
            .WithName("CompleteTask")
            .RequireAuthorization("execution.write");

        group.MapPost("/{taskRunId:guid}:skip", SkipTaskAsync)
            .WithName("SkipTask")
            .RequireAuthorization("execution.write");

        group.MapPost("/{taskRunId:guid}/evidence", AttachEvidenceAsync)
            .WithName("AttachEvidence")
            .RequireAuthorization("execution.write");
    }

    private static async Task<IResult> TakeTaskAsync(
        Guid taskRunId,
        TakeTaskRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new TakeTaskCommand(taskRunId, request?.PersonId, request?.RoleId, request?.Mode ?? "individual"),
            cancellationToken);

        return result.IsSuccess ? Results.Ok() : result.ToProblem();
    }

    private static async Task<IResult> StartTaskAsync(
        Guid taskRunId,
        StartTaskRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartTaskCommand(taskRunId, request?.OperatorId), cancellationToken);

        return result.IsSuccess ? Results.Ok() : result.ToProblem();
    }

    private static async Task<IResult> ReportProgressAsync(
        Guid taskRunId,
        ReportProgressRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReportProgressCommand(taskRunId, request.Method, request.ProgressPct, request.Quantity, request.TargetQuantity),
            cancellationToken);

        return result.IsSuccess ? Results.Ok() : result.ToProblem();
    }

    private static async Task<IResult> BlockTaskAsync(
        Guid taskRunId,
        BlockTaskRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new BlockTaskCommand(taskRunId, request.Cause, request.ReasonCodeId),
            cancellationToken);

        return result.IsSuccess ? Results.Ok() : result.ToProblem();
    }

    private static async Task<IResult> UnblockTaskAsync(
        Guid taskRunId,
        UnblockTaskRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnblockTaskCommand(taskRunId, request?.Resolution), cancellationToken);

        return result.IsSuccess ? Results.Ok() : result.ToProblem();
    }

    private static async Task<IResult> CompleteTaskAsync(
        Guid taskRunId,
        CompleteTaskRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CompleteTaskCommand(taskRunId, request?.Force ?? false, request?.Reason),
            cancellationToken);

        return result.IsSuccess ? Results.Ok() : result.ToProblem();
    }

    private static async Task<IResult> SkipTaskAsync(
        Guid taskRunId,
        SkipTaskRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SkipTaskCommand(taskRunId, request.Reason, request.AuthorizedBy),
            cancellationToken);

        return result.IsSuccess ? Results.Ok() : result.ToProblem();
    }

    private static async Task<IResult> AttachEvidenceAsync(
        Guid taskRunId,
        AttachEvidenceRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AttachEvidenceCommand(
                taskRunId,
                request.Kind,
                request.Status,
                request.FileId,
                request.MediaRef,
                request.ContentHash,
                request.RequirementId,
                request.CapturedBy,
                request.Caption),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/v1/tasks/{taskRunId}/evidence/{result.Value.Id}", result.Value)
            : result.ToProblem();
    }
}
