namespace Nexo.BuildingBlocks.MultiTenancy;

/// <summary>Ambient, per-request information about the tenant that owns the current operation.</summary>
public interface ITenantContext
{
    Guid TenantId { get; }

    string TenantKey { get; }

    bool IsResolved { get; }

    void Set(Guid tenantId, string tenantKey);
}
