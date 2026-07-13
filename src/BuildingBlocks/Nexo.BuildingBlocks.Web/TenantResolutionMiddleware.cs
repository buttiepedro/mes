using Microsoft.AspNetCore.Http;
using Nexo.BuildingBlocks.MultiTenancy;

namespace Nexo.BuildingBlocks.Web;

/// <summary>
/// Resolves the current tenant from the <c>tenant_id</c> JWT claim, falling back to the
/// <c>X-Tenant-Key</c> header (useful in development / behind a trusted gateway), and stores
/// it on the scoped <see cref="ITenantContext"/>. Endpoints that require a tenant should reject
/// unresolved requests with 400/401.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private const string TenantIdClaim = "tenant_id";
    private const string TenantKeyHeader = "X-Tenant-Key";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var tenantIdValue = context.User.FindFirst(TenantIdClaim)?.Value;
        var tenantKey = context.Request.Headers[TenantKeyHeader].FirstOrDefault();

        if (Guid.TryParse(tenantIdValue, out var tenantId))
        {
            tenantContext.Set(tenantId, string.IsNullOrWhiteSpace(tenantKey) ? tenantIdValue! : tenantKey);
        }
        else if (!string.IsNullOrWhiteSpace(tenantKey))
        {
            // Development / gateway fallback: only the tenant key is known here; the connection
            // resolver fills in the real TenantId when it looks the tenant up by key.
            tenantContext.Set(Guid.Empty, tenantKey);
        }

        await _next(context);
    }
}
