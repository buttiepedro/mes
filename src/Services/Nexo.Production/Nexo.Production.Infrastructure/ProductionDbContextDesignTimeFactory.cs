using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexo.BuildingBlocks.MultiTenancy;

namespace Nexo.Production.Infrastructure;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> can build a <see cref="ProductionDbContext"/>
/// without the full DI graph. Uses <c>PRODUCTION_DB_CONNECTION</c> (or a local default).
/// </summary>
public sealed class ProductionDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ProductionDbContext>
{
    public ProductionDbContext CreateDbContext(string[] args)
    {
        // Puerto 5433: es el que expone el Postgres de docker-compose en el host
        // (el 5432 suele estar tomado por un PostgreSQL nativo instalado en la máquina).
        var connectionString = Environment.GetEnvironmentVariable("PRODUCTION_DB_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=nexo_tenant_demo;Username=nexo;Password=nexo";

        var options = new DbContextOptionsBuilder<ProductionDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", ProductionDbContext.DomainSchema))
            .Options;

        return new ProductionDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;

        public string TenantKey => string.Empty;

        public bool IsResolved => false;

        public void Set(Guid tenantId, string tenantKey)
        {
        }
    }
}
