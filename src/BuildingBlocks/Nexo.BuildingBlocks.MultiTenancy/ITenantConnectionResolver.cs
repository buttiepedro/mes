namespace Nexo.BuildingBlocks.MultiTenancy;

/// <summary>Resolves the connection details for a tenant given its key.</summary>
public interface ITenantConnectionResolver
{
    Task<TenantConnectionInfo?> ResolveAsync(string tenantKey, CancellationToken cancellationToken = default);
}
