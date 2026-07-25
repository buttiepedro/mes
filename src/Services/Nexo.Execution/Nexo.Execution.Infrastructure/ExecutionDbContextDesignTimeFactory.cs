using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexo.BuildingBlocks.MultiTenancy;

namespace Nexo.Execution.Infrastructure;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> / <c>dotnet ef dbcontext info</c> can build an
/// <see cref="ExecutionDbContext"/> without the full DI graph. Uses <c>EXECUTION_DB_CONNECTION</c>
/// (or a local default).
/// </summary>
public sealed class ExecutionDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ExecutionDbContext>
{
    public ExecutionDbContext CreateDbContext(string[] args)
    {
        // Puerto 5433: es el que expone el Postgres de docker-compose en el host
        // (el 5432 suele estar tomado por un PostgreSQL nativo instalado en la máquina).
        var connectionString = Environment.GetEnvironmentVariable("EXECUTION_DB_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=nexo_tenant_demo;Username=nexo;Password=nexo";

        var options = new DbContextOptionsBuilder<ExecutionDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", ExecutionDbContext.DomainSchema))
            .Options;

        return new ExecutionDbContext(options, new DesignTimeTenantContext());
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
