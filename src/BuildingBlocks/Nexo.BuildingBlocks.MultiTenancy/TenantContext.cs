namespace Nexo.BuildingBlocks.MultiTenancy;

/// <summary>Scoped implementation of <see cref="ITenantContext"/>, set once per request.</summary>
public sealed class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }

    public string TenantKey { get; private set; } = string.Empty;

    public bool IsResolved { get; private set; }

    public void Set(Guid tenantId, string tenantKey)
    {
        TenantId = tenantId;
        TenantKey = tenantKey ?? throw new ArgumentNullException(nameof(tenantKey));
        IsResolved = true;
    }
}
