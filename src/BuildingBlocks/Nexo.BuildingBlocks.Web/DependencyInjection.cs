using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Nexo.BuildingBlocks.Web;

/// <summary>Registration and pipeline helpers for the shared web building block.</summary>
public static class DependencyInjection
{
    /// <summary>Registers services required by <see cref="UseNexoWeb"/>.</summary>
    public static IServiceCollection AddNexoWeb(this IServiceCollection services)
    {
        services.AddProblemDetails();

        return services;
    }

    /// <summary>
    /// Adds the shared middleware pipeline: exception handling (outermost) followed by
    /// tenant resolution.
    /// </summary>
    public static IApplicationBuilder UseNexoWeb(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<TenantResolutionMiddleware>();

        return app;
    }
}
