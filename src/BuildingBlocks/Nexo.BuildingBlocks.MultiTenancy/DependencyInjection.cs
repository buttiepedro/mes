using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nexo.BuildingBlocks.MultiTenancy;

/// <summary>Registration helpers for the multi-tenancy building block.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the scoped <see cref="ITenantContext"/> and the configuration-backed
    /// <see cref="ITenantConnectionResolver"/>.
    /// </summary>
    public static IServiceCollection AddMultiTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddSingleton<ITenantConnectionResolver, ConfigurationTenantConnectionResolver>();

        return services;
    }
}
