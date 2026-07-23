using Microsoft.Extensions.Configuration;

namespace Nexo.BuildingBlocks.MultiTenancy;

/// <summary>
/// Local scaffold resolver that reads tenants from the "Tenants" configuration section, where each
/// entry has the shape { Key, TenantId, ConnectionString, SchemaVersion }.
/// </summary>
/// <remarks>
/// The production resolver backed by the Tenant Connection Registry (Neon control plane +
/// AWS Secrets Manager) is intentionally out of scope for the scaffold.
/// TODO: implement the Registry-backed resolver described in docs/design/01-multi-tenancy-connection.md.
/// </remarks>
public sealed class ConfigurationTenantConnectionResolver : ITenantConnectionResolver
{
    private const string SectionName = "Tenants";

    private readonly IConfiguration _configuration;

    public ConfigurationTenantConnectionResolver(IConfiguration configuration) => _configuration = configuration;

    public Task<TenantConnectionInfo?> ResolveAsync(string tenantKey, CancellationToken cancellationToken = default)
    {
        foreach (var section in _configuration.GetSection(SectionName).GetChildren())
        {
            var key = section["Key"];

            if (!string.Equals(key, tenantKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Guid.TryParse(section["TenantId"], out var tenantId);

            var info = new TenantConnectionInfo(
                tenantId,
                key ?? tenantKey,
                section["ConnectionString"] ?? string.Empty,
                section["SchemaVersion"] ?? string.Empty);

            return Task.FromResult<TenantConnectionInfo?>(info);
        }

        return Task.FromResult<TenantConnectionInfo?>(null);
    }
}
