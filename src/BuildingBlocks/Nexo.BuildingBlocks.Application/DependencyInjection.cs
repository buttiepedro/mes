using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Nexo.BuildingBlocks.Application.Behaviors;

namespace Nexo.BuildingBlocks.Application;

/// <summary>Registration helpers for the application layer.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR handlers from <paramref name="assembly"/> together with the
    /// validation and logging pipeline behaviors.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, Assembly assembly)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
