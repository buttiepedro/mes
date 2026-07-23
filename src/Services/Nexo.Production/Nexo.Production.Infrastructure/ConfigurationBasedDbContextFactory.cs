using Microsoft.Extensions.Configuration;
using Nexo.BuildingBlocks.MultiTenancy;

namespace Nexo.Production.Infrastructure;

/// <summary>
/// Resolves the Postgres connection string for the current tenant using the
/// <see cref="ITenantConnectionResolver"/> (local: <c>ConfigurationTenantConnectionResolver</c> over the
/// <c>Tenants</c> section) keyed by the <see cref="ITenantContext"/>.
/// </summary>
/// <remarks>
/// Scoped: read at DbContext options build time per request (see <see cref="DependencyInjection"/>).
/// The productive resolver based on the Tenant Connection Registry / Neon is a TODO
/// (see docs/design/01-multi-tenancy-connection.md).
/// </remarks>
public sealed class ConfigurationBasedDbContextFactory
{
    private const string DefaultConnectionName = "ProductionDefault";

    private readonly ITenantContext _tenantContext;
    private readonly ITenantConnectionResolver _connectionResolver;
    private readonly IConfiguration _configuration;

    public ConfigurationBasedDbContextFactory(
        ITenantContext tenantContext,
        ITenantConnectionResolver connectionResolver,
        IConfiguration configuration)
    {
        _tenantContext = tenantContext;
        _connectionResolver = connectionResolver;
        _configuration = configuration;
    }

    public string GetConnectionString()
    {
        if (_tenantContext.IsResolved)
        {
            var info = _connectionResolver
                .ResolveAsync(_tenantContext.TenantKey)
                .GetAwaiter()
                .GetResult();

            if (info is not null && !string.IsNullOrWhiteSpace(info.ConnectionString))
            {
                return info.ConnectionString;
            }
        }

        // Fallback used for design-time/migrations and unresolved (no-tenant) requests.
        return _configuration.GetConnectionString(DefaultConnectionName)
            ?? throw new InvalidOperationException(
                $"No connection string resolved for the current tenant and no '{DefaultConnectionName}' fallback configured.");
    }
}
