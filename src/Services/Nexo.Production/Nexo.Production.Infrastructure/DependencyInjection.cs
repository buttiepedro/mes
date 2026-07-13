using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexo.BuildingBlocks.Application;
using Nexo.Production.Application;

namespace Nexo.Production.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Production EF Core stack: a per-tenant <see cref="ProductionDbContext"/> whose
    /// connection string is resolved at request time by <see cref="ConfigurationBasedDbContextFactory"/>,
    /// exposed to the Application layer as <see cref="IProductionDbContext"/> and <see cref="IUnitOfWork"/>.
    /// </summary>
    public static IServiceCollection AddProductionInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ConfigurationBasedDbContextFactory>();

        services.AddDbContext<ProductionDbContext>(
            (serviceProvider, options) =>
            {
                var factory = serviceProvider.GetRequiredService<ConfigurationBasedDbContextFactory>();
                options.UseNpgsql(
                    factory.GetConnectionString(),
                    npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ProductionDbContext.DomainSchema));
            },
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);

        services.AddScoped<IProductionDbContext>(sp => sp.GetRequiredService<ProductionDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ProductionDbContext>());

        return services;
    }
}
