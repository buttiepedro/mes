namespace Nexo.BuildingBlocks.MultiTenancy;

/// <summary>Resolved connection details for a tenant's dedicated database.</summary>
public sealed record TenantConnectionInfo(
    Guid TenantId,
    string TenantKey,
    string ConnectionString,
    string SchemaVersion);
