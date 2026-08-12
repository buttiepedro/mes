using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Nexo.BuildingBlocks.Web;

/// <summary>
/// Validates JWTs issued by <b>HEXA</b> (the ERP) so the MES trusts HEXA as its identity provider — the
/// MES has no IdP of its own. HEXA signs <b>HS256</b> with a shared secret and puts the user in
/// <c>sub</c>, the tenant in <c>company_id</c> and the role in <c>role</c> (HEXA `auth-multitenancy` spec).
/// </summary>
/// <remarks>
/// This handler: (1) validates the HS256 signature + expiry against the shared secret; (2) maps
/// <c>company_id</c> → the <c>tenant_id</c> claim that <see cref="TenantResolutionMiddleware"/> reads;
/// (3) derives Nexo scopes from the HEXA <c>role</c> (owner/admin/editor → read+write, otherwise read),
/// since HEXA tokens carry no per-scope claim. Enable it with <c>Auth:Mode=HexaJwt</c>; the shared secret
/// comes from <c>Hexa:JwtSecret</c> (fallback <c>JWT_SECRET</c>). This is the seam that replaces the
/// abandoned Duende plan — see docs/design/hexa-integration/README.md.
/// <para>
/// ⚠️ Interop gotcha: for HS256, Microsoft.IdentityModel enforces RFC 7518 §3.2 — the shared secret MUST
/// be at least 256 bits (32 bytes), otherwise every token fails signature validation with IDX10503.
/// HEXA's real JWT_SECRET ("a long random string") satisfies this; HEXA's dev default
/// (<c>insecure-jwt-secret</c>, 19 bytes) does NOT. Align on a ≥32-byte shared secret.
/// </para>
/// </remarks>
public static class HexaAuthentication
{
    private const string TenantIdClaim = "tenant_id";

    private static readonly string[] WriteScopes =
    {
        "nexo.masterdata.read", "nexo.masterdata.write",
        "nexo.workmodel.read", "nexo.workmodel.write",
        "nexo.execution.read", "nexo.execution.write",
        "nexo.production.read", "nexo.production.write",
    };

    private static readonly string[] ReadScopes =
    {
        "nexo.masterdata.read", "nexo.workmodel.read", "nexo.execution.read", "nexo.production.read",
    };

    public static AuthenticationBuilder AddNexoHexaJwt(this IServiceCollection services, IConfiguration configuration)
    {
        // Matches HEXA's own dev default (`config.py` JWT_SECRET); override in every real deployment.
        var secret = configuration["Hexa:JwtSecret"]
            ?? configuration["JWT_SECRET"]
            ?? "insecure-jwt-secret";
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        return services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep raw HEXA claim names: sub, company_id, role
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = false,   // HEXA access tokens carry no iss/aud
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = static context =>
                    {
                        if (context.Principal?.Identity is not ClaimsIdentity identity)
                        {
                            return Task.CompletedTask;
                        }

                        // company_id → tenant_id (the tenant middleware reads tenant_id)
                        var companyId = identity.FindFirst("company_id")?.Value;
                        if (!string.IsNullOrWhiteSpace(companyId) && identity.FindFirst(TenantIdClaim) is null)
                        {
                            identity.AddClaim(new Claim(TenantIdClaim, companyId));
                        }

                        // HEXA role → Nexo scopes (HEXA has no per-scope claim)
                        if (identity.FindFirst("scope") is null)
                        {
                            var role = identity.FindFirst("role")?.Value?.ToLowerInvariant();
                            var scopes = role is "owner" or "admin" or "editor" ? WriteScopes : ReadScopes;
                            identity.AddClaim(new Claim("scope", string.Join(' ', scopes)));
                        }

                        return Task.CompletedTask;
                    },
                };
            });
    }
}
