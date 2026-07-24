using MediatR;
using Nexo.WorkModel.Application;

namespace Nexo.WorkModel.Api;

/// <summary>
/// Minimal API endpoints for the Work Model slice under <c>/v1</c>
/// (docs/design/04-service-contracts.md §2.6). Reads require <c>nexo.workmodel.read</c>, structural
/// writes <c>nexo.workmodel.write</c>, and publishing/suspending <c>nexo.workmodel.publish</c>
/// (segregation of duties).
/// </summary>
public static class WorkModelEndpoints
{
    public static IEndpointRouteBuilder MapWorkModelEndpoints(this IEndpointRouteBuilder app)
    {
        MapProcessEndpoints(app);
        MapVersionEndpoints(app);
        MapTaskEndpoints(app);

        return app;
    }

    // --- Processes ----------------------------------------------------------------------------

    private static void MapProcessEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/processes").WithTags("Work Model · Processes");

        group.MapGet("/", ListProcessesAsync)
            .WithName("ListProcesses")
            .RequireAuthorization("workmodel.read");

        group.MapPost("/", CreateProcessAsync)
            .WithName("CreateProcess")
            .RequireAuthorization("workmodel.write");
    }

    private static async Task<IResult> ListProcessesAsync(
        ISender sender,
        CancellationToken cancellationToken,
        string? profile = null,
        string? status = null,
        string? q = null,
        int limit = PagingDefaults.DefaultLimit,
        int offset = 0)
    {
        var result = await sender.Send(new ListProcessesQuery(profile, status, q, limit, offset), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> CreateProcessAsync(
        CreateProcessRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateProcessCommand(
                request.Code,
                request.Name,
                request.Profile,
                request.OutputItemId,
                request.OutputUomId,
                request.SiteId,
                request.AreaId,
                request.LineId,
                request.EvidencePolicy,
                request.SkipPolicy,
                request.Tags,
                request.ExternalRef),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/v1/processes/{result.Value.ProcessId}", result.Value)
            : result.ToProblem();
    }

    // --- Versions -----------------------------------------------------------------------------

    private static void MapVersionEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/processes/{processId:guid}/versions").WithTags("Work Model · Versions");

        group.MapGet("/", ListVersionsAsync)
            .WithName("ListProcessVersions")
            .RequireAuthorization("workmodel.read");

        group.MapPost("/", CreateDraftVersionAsync)
            .WithName("CreateDraftVersion")
            .RequireAuthorization("workmodel.write");

        // The published version with its complete graph — what Execution reads to freeze a run.
        group.MapGet("/published", GetPublishedVersionAsync)
            .WithName("GetPublishedVersion")
            .RequireAuthorization("workmodel.read");

        group.MapPut("/{versionId:guid}/graph", SetGraphAsync)
            .WithName("SetVersionGraph")
            .RequireAuthorization("workmodel.write");

        // Read-only integral validation of the version (the editor's live check): read scope is enough.
        group.MapPost("/{versionId:guid}:validate", ValidateVersionAsync)
            .WithName("ValidateVersion")
            .RequireAuthorization("workmodel.read");

        group.MapPost("/{versionId:guid}:publish", PublishVersionAsync)
            .WithName("PublishVersion")
            .RequireAuthorization("workmodel.publish");

        group.MapPost("/{versionId:guid}:suspend", SuspendVersionAsync)
            .WithName("SuspendVersion")
            .RequireAuthorization("workmodel.publish");
    }

    private static async Task<IResult> ListVersionsAsync(Guid processId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListProcessVersionsQuery(processId), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> CreateDraftVersionAsync(
        Guid processId,
        CreateDraftVersionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateDraftVersionCommand(processId, request.Bump, request.ChangeReason),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/v1/processes/{processId}/versions/{result.Value.Id}", result.Value)
            : result.ToProblem();
    }

    private static async Task<IResult> GetPublishedVersionAsync(Guid processId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPublishedVersionQuery(processId), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> SetGraphAsync(
        Guid processId,
        Guid versionId,
        SetGraphRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SetGraphCommand(processId, versionId, request.Edges ?? Array.Empty<GraphEdgeRequest>()),
            cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> ValidateVersionAsync(
        Guid processId,
        Guid versionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ValidateVersionCommand(processId, versionId), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> PublishVersionAsync(
        Guid processId,
        Guid versionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishVersionCommand(processId, versionId), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> SuspendVersionAsync(
        Guid processId,
        Guid versionId,
        SuspendVersionRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SuspendVersionCommand(processId, versionId, request?.Reason),
            cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    // --- Tasks --------------------------------------------------------------------------------

    private static void MapTaskEndpoints(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/processes/{processId:guid}/versions/{versionId:guid}/tasks")
            .WithTags("Work Model · Tasks");

        group.MapGet("/", ListVersionTasksAsync)
            .WithName("ListVersionTasks")
            .RequireAuthorization("workmodel.read");

        group.MapPost("/", AddTaskAsync)
            .WithName("AddTask")
            .RequireAuthorization("workmodel.write");

        group.MapDelete("/{taskId:guid}", RemoveTaskAsync)
            .WithName("RemoveTask")
            .RequireAuthorization("workmodel.write");
    }

    private static async Task<IResult> ListVersionTasksAsync(
        Guid processId,
        Guid versionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListVersionTasksQuery(processId, versionId), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> AddTaskAsync(
        Guid processId,
        Guid versionId,
        AddTaskRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddTaskCommand(
                processId,
                versionId,
                request.Code,
                request.Name,
                request.ResponsibleRoleId,
                request.CompletionKind,
                request.CompletionSpec,
                request.EstimatedDurationSeconds,
                request.StandardDurationSeconds,
                request.ProgressWeight,
                request.Obligation,
                request.IsMilestone,
                request.IsParallelizable,
                request.IsRepeatable,
                request.EvidencePolicy,
                request.RequiredEvidenceKind,
                request.MinEvidenceCount,
                request.RequiredCapability,
                request.RequiredAssetType,
                request.Instructions,
                request.DisplaySeq,
                request.Inputs),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created(
                $"/v1/processes/{processId}/versions/{versionId}/tasks/{result.Value.Id}",
                result.Value)
            : result.ToProblem();
    }

    private static async Task<IResult> RemoveTaskAsync(
        Guid processId,
        Guid versionId,
        Guid taskId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveTaskCommand(processId, versionId, taskId), cancellationToken);

        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }
}
