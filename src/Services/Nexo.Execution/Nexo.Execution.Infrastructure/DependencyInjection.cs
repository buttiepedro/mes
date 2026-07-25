using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexo.BuildingBlocks.Application;
using Nexo.Execution.Application;

namespace Nexo.Execution.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Execution EF Core stack: a per-tenant <see cref="ExecutionDbContext"/> whose
    /// connection string is resolved at request time by <see cref="ConfigurationBasedDbContextFactory"/>,
    /// exposed to the Application layer as <see cref="IExecutionDbContext"/> and <see cref="IUnitOfWork"/>.
    /// </summary>
    public static IServiceCollection AddExecutionInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ConfigurationBasedDbContextFactory>();

        services.AddDbContext<ExecutionDbContext>(
            (serviceProvider, options) =>
            {
                var factory = serviceProvider.GetRequiredService<ConfigurationBasedDbContextFactory>();
                options.UseNpgsql(
                    factory.GetConnectionString(),
                    npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ExecutionDbContext.DomainSchema));
            },
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);

        services.AddScoped<IExecutionDbContext>(sp => sp.GetRequiredService<ExecutionDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ExecutionDbContext>());

        return services;
    }
}
