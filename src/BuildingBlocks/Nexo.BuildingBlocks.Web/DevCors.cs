using Microsoft.Extensions.DependencyInjection;

namespace Nexo.BuildingBlocks.Web;

/// <summary>
/// Development-only CORS so the local console (served from one service) can call the APIs of the others
/// on different ports. Permissive by design and NEVER meant for production, where the front-end is served
/// same-origin (or behind a gateway) and CORS is configured explicitly.
/// </summary>
public static class DevCors
{
    public const string PolicyName = "nexo-dev-cors";

    public static IServiceCollection AddNexoDevCors(this IServiceCollection services)
        => services.AddCors(options => options.AddPolicy(
            PolicyName,
            policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
}
