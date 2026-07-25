using Nexo.BuildingBlocks.Domain;

namespace Nexo.Execution.Api;

/// <summary>Maps a failed <see cref="Result"/> to an RFC7807 ProblemDetails response.</summary>
/// <remarks>
/// The status code is inferred from the canonical error code suffix (docs/design/04-service-contracts.md):
/// <c>NotFound</c> → 404, <c>Conflict</c> → 409, <c>Invalid</c> → 422, everything else → 400.
/// </remarks>
public static class ResultExtensions
{
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok();
        }

        var error = result.Error;

        var statusCode = error.Code switch
        {
            _ when error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status404NotFound,
            _ when error.Code.Contains("Conflict", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
            _ when error.Code.Contains("Invalid", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            title: error.Message,
            statusCode: statusCode,
            type: $"https://problems.nexo.app/{error.Code.Replace('.', '/').ToLowerInvariant()}",
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
