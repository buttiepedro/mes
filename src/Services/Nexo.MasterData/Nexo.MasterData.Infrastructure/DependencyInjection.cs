using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexo.BuildingBlocks.Application;
using Nexo.MasterData.Application;

namespace Nexo.MasterData.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Master Data EF Core stack: a per-tenant <see cref="MasterDataDbContext"/> whose
    /// connection string is resolved at request time by <see cref="ConfigurationBasedDbContextFactory"/>,
    /// exposed to the Application layer as <see cref="IMasterDataDbContext"/> and <see cref="IUnitOfWork"/>.
    /// </summary>
    public static IServiceCollection AddMasterDataInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ConfigurationBasedDbContextFactory>();

        services.AddDbContext<MasterDataDbContext>(
            (serviceProvider, options) =>
            {
                var factory = serviceProvider.GetRequiredService<ConfigurationBasedDbContextFactory>();
                options.UseNpgsql(
                    factory.GetConnectionString(),
                    npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", MasterDataDbContext.DomainSchema));
            },
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);

        services.AddScoped<IMasterDataDbContext>(sp => sp.GetRequiredService<MasterDataDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MasterDataDbContext>());

        return services;
    }
}
