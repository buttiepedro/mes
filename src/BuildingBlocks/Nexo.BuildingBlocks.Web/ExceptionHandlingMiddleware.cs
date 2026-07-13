using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Nexo.BuildingBlocks.Web;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 ProblemDetails responses:
/// FluentValidation's ValidationException -> 400 (with per-property errors),
/// <see cref="KeyNotFoundException"/> -> 404, everything else -> 500.
/// </summary>
/// <remarks>
/// This building block deliberately has no compile-time reference to FluentValidation
/// (see the project's package list), so the validation exception is matched by type name
/// and its failures are read reflectively.
/// </remarks>
public sealed class ExceptionHandlingMiddleware
{
    private const string ValidationExceptionTypeName = "FluentValidation.ValidationException";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var problem = CreateProblemDetails(exception);

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(
            problem,
            options: (System.Text.Json.JsonSerializerOptions?)null,
            contentType: "application/problem+json");
    }

    private static ProblemDetails CreateProblemDetails(Exception exception)
    {
        if (TryGetValidationErrors(exception, out var errors))
        {
            var validationProblem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Detail = "One or more validation errors occurred.",
            };

            validationProblem.Extensions["errors"] = errors;
            return validationProblem;
        }

        return exception switch
        {
            KeyNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Detail = exception.Message,
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Detail = "An unexpected error occurred while processing the request.",
            },
        };
    }

    private static bool TryGetValidationErrors(Exception exception, out IDictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();

        if (exception.GetType().FullName != ValidationExceptionTypeName)
        {
            return false;
        }

        if (exception.GetType().GetProperty("Errors")?.GetValue(exception) is not System.Collections.IEnumerable failures)
        {
            return true;
        }

        var grouped = new Dictionary<string, List<string>>();

        foreach (var failure in failures)
        {
            if (failure is null)
            {
                continue;
            }

            var failureType = failure.GetType();
            var propertyName = failureType.GetProperty("PropertyName")?.GetValue(failure) as string ?? string.Empty;
            var message = failureType.GetProperty("ErrorMessage")?.GetValue(failure) as string ?? string.Empty;

            if (!grouped.TryGetValue(propertyName, out var messages))
            {
                messages = new List<string>();
                grouped[propertyName] = messages;
            }

            messages.Add(message);
        }

        errors = grouped.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        return true;
    }
}
