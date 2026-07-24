using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexo.BuildingBlocks.Application;
using Nexo.WorkModel.Application;

namespace Nexo.WorkModel.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Work Model EF Core stack: a per-tenant <see cref="WorkModelDbContext"/> whose
    /// connection string is resolved at request time by <see cref="ConfigurationBasedDbContextFactory"/>,
    /// exposed to the Application layer as <see cref="IWorkModelDbContext"/> and <see cref="IUnitOfWork"/>.
    /// </summary>
    public static IServiceCollection AddWorkModelInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ConfigurationBasedDbContextFactory>();

        services.AddDbContext<WorkModelDbContext>(
            (serviceProvider, options) =>
            {
                var factory = serviceProvider.GetRequiredService<ConfigurationBasedDbContextFactory>();
                options.UseNpgsql(
                    factory.GetConnectionString(),
                    npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", WorkModelDbContext.DomainSchema));
            },
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);

        services.AddScoped<IWorkModelDbContext>(sp => sp.GetRequiredService<WorkModelDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WorkModelDbContext>());

        return services;
    }
}
