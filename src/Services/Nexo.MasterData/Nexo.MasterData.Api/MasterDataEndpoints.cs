using MediatR;
using Nexo.MasterData.Application;

namespace Nexo.MasterData.Api;

/// <summary>
/// Minimal API endpoints for the Master Data slice under <c>/v1</c>
/// (docs/design/04-service-contracts.md §2.5).
/// </summary>
public static class MasterDataEndpoints
{
    public static IEndpointRouteBuilder MapMasterDataEndpoints(this IEndpointRouteBuilder app)
    {
        MapUomEndpoints(app);
        MapItemEndpoints(app);
        MapPeopleEndpoints(app);
        MapCustomerEndpoints(app);

        return app;
    }

    // --- Units of measure -------------------------------------------------------------------

    private static void MapUomEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/uoms").WithTags("Master Data · Units of measure");

        group.MapGet("/", ListUomsAsync)
            .WithName("ListUoms")
            .RequireAuthorization("masterdata.read");

        group.MapGet("/{uomId:guid}", GetUomAsync)
            .WithName("GetUom")
            .RequireAuthorization("masterdata.read");

        group.MapPost("/", CreateUomAsync)
            .WithName("CreateUom")
            .RequireAuthorization("masterdata.write");
    }

    private static async Task<IResult> ListUomsAsync(
        ISender sender,
        CancellationToken cancellationToken,
        string? magnitude = null,
        string? status = null,
        int limit = PagingDefaults.DefaultLimit,
        int offset = 0)
    {
        var result = await sender.Send(new ListUomsQuery(magnitude, status, limit, offset), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> GetUomAsync(Guid uomId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUomQuery(uomId), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> CreateUomAsync(
        CreateUomRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateUomCommand(
                request.Code,
                request.Name,
                request.Symbol,
                request.Magnitude,
                request.FactorToBase,
                request.IsBase,
                request.Decimals,
                request.ExternalRef),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/v1/uoms/{result.Value}", new { id = result.Value })
            : result.ToProblem();
    }

    // --- Items ------------------------------------------------------------------------------

    private static void MapItemEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/items").WithTags("Master Data · Items");

        group.MapGet("/", ListItemsAsync)
            .WithName("ListItems")
            .RequireAuthorization("masterdata.read");

        group.MapGet("/{itemId:guid}", GetItemAsync)
            .WithName("GetItem")
            .RequireAuthorization("masterdata.read");

        group.MapPost("/", CreateItemAsync)
            .WithName("CreateItem")
            .RequireAuthorization("masterdata.write");

        group.MapPut("/{itemId:guid}", UpdateItemAsync)
            .WithName("UpdateItem")
            .RequireAuthorization("masterdata.write");

        // Logical removal: never a physical delete when events reference the item (R4).
        group.MapPost("/{itemId:guid}:archive", ArchiveItemAsync)
            .WithName("ArchiveItem")
            .RequireAuthorization("masterdata.admin");
    }

    private static async Task<IResult> ListItemsAsync(
        ISender sender,
        CancellationToken cancellationToken,
        string? role = null,
        string? status = null,
        string? q = null,
        int limit = PagingDefaults.DefaultLimit,
        int offset = 0)
    {
        var result = await sender.Send(new ListItemsQuery(role, status, q, limit, offset), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> GetItemAsync(Guid itemId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetItemQuery(itemId), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> CreateItemAsync(
        CreateItemRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateItemCommand(
                request.Code,
                request.Name,
                request.BaseUom,
                request.Roles ?? DefaultRoles,
                request.Tracking,
                request.Category,
                request.Family,
                request.IdealCycleTime,
                request.DefaultProcessId,
                request.QualitySpecs,
                request.ExternalRef),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/v1/items/{result.Value}", new { id = result.Value })
            : result.ToProblem();
    }

    private static async Task<IResult> UpdateItemAsync(
        Guid itemId,
        UpdateItemRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateItemCommand(
                itemId,
                request.Name,
                request.Roles ?? DefaultRoles,
                request.Tracking,
                request.Category,
                request.Family,
                request.IdealCycleTime,
                request.DefaultProcessId,
                request.QualitySpecs),
            cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> ArchiveItemAsync(Guid itemId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveItemCommand(itemId), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    // --- People -----------------------------------------------------------------------------

    private static void MapPeopleEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/people").WithTags("Master Data · People");

        group.MapGet("/", ListPeopleAsync)
            .WithName("ListPeople")
            .RequireAuthorization("masterdata.read");

        group.MapGet("/{personId:guid}", GetPersonAsync)
            .WithName("GetPerson")
            .RequireAuthorization("masterdata.read");

        group.MapPost("/", CreatePersonAsync)
            .WithName("CreatePerson")
            .RequireAuthorization("masterdata.write");
    }

    private static async Task<IResult> ListPeopleAsync(
        ISender sender,
        CancellationToken cancellationToken,
        string? status = null,
        string? q = null,
        int limit = PagingDefaults.DefaultLimit,
        int offset = 0)
    {
        var result = await sender.Send(new ListPeopleQuery(status, q, limit, offset), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> GetPersonAsync(Guid personId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPersonQuery(personId), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> CreatePersonAsync(
        CreatePersonRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreatePersonCommand(
                request.Code,
                request.FullName,
                request.DefaultRoleId,
                request.SiteId,
                request.LineId,
                request.UserId,
                request.Calendar,
                request.ExternalRef),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/v1/people/{result.Value}", new { id = result.Value })
            : result.ToProblem();
    }

    // --- Customers --------------------------------------------------------------------------

    private static void MapCustomerEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/customers").WithTags("Master Data · Customers");

        group.MapGet("/", ListCustomersAsync)
            .WithName("ListCustomers")
            .RequireAuthorization("masterdata.read");

        group.MapGet("/{customerId:guid}", GetCustomerAsync)
            .WithName("GetCustomer")
            .RequireAuthorization("masterdata.read");

        group.MapPost("/", CreateCustomerAsync)
            .WithName("CreateCustomer")
            .RequireAuthorization("masterdata.write");
    }

    private static async Task<IResult> ListCustomersAsync(
        ISender sender,
        CancellationToken cancellationToken,
        string? status = null,
        string? q = null,
        int limit = PagingDefaults.DefaultLimit,
        int offset = 0)
    {
        var result = await sender.Send(new ListCustomersQuery(status, q, limit, offset), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> GetCustomerAsync(Guid customerId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCustomerQuery(customerId), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<IResult> CreateCustomerAsync(
        CreateCustomerRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCustomerCommand(
                request.Code,
                request.LegalName,
                request.TaxId,
                request.Contact,
                request.Notes,
                request.ExternalRef),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/v1/customers/{result.Value}", new { id = result.Value })
            : result.ToProblem();
    }

    /// <summary>Same default as the <c>roles</c> column: an item with no explicit role is an input.</summary>
    private static readonly string[] DefaultRoles = { "input" };
}
