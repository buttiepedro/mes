using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexo.BuildingBlocks.MultiTenancy;
using Nexo.MesApi.Application;

namespace Nexo.MesApi.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra el <see cref="MesConfigDbContext"/> resolviendo la conexión del tenant actual
    /// (por <see cref="ITenantContext"/> + <see cref="ITenantConnectionResolver"/>) y lo expone como
    /// puerto <see cref="IMesConfigDbContext"/>.
    /// </summary>
    public static IServiceCollection AddMesInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<MesConfigDbContext>((sp, options) =>
        {
            var tenant = sp.GetRequiredService<ITenantContext>();
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var info = resolver.ResolveAsync(tenant.TenantKey).GetAwaiter().GetResult();

            // En runtime el TenantResolutionMiddleware resuelve el tenant antes de tocar el DbContext.
            // Sin tenant resuelto (EF tools / diseño de migraciones) se cae al connection de diseño local.
            // TODO(hardening): lanzar en entornos productivos si el tenant no resuelve.
            var connectionString = info?.ConnectionString is { Length: > 0 } cs
                ? cs
                : "Host=localhost;Port=5433;Database=nexo_tenant_demo;Username=nexo;Password=nexo";

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IMesConfigDbContext>(sp => sp.GetRequiredService<MesConfigDbContext>());

        return services;
    }
}
