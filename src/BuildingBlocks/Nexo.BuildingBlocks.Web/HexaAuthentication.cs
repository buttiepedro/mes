using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Nexo.BuildingBlocks.Web;

/// <summary>
/// Valida los JWT emitidos por HEXA (el ERP) — HEXA es el IdP del MES, no hay IdP propio. HEXA firma
/// HS256 con un secreto compartido y pone el usuario en <c>sub</c>, el tenant en <c>company_id</c> y el
/// rol en <c>role</c>. Este handler: valida firma HS256 + expiración; mapea <c>company_id</c> → el claim
/// <c>tenant_id</c> que lee el tenant-middleware del MES; y deriva scopes Nexo del <c>role</c> de HEXA
/// (owner/admin/editor → read+write; resto → read). Se activa con <c>Auth:Mode=HexaJwt</c>; el secreto
/// viene de <c>Hexa:JwtSecret</c>.
/// </summary>
public static class HexaAuthentication
{
    public const string TenantIdClaim = "tenant_id";

    private static readonly string[] WriteScopes =
    {
        "nexo.mes.read", "nexo.mes.write",
    };

    private static readonly string[] ReadScopes = { "nexo.mes.read" };

    public static AuthenticationBuilder AddNexoHexaJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var secret = configuration["Hexa:JwtSecret"]
            ?? configuration["JWT_SECRET"]
            ?? "insecure-jwt-secret"; // igual al default dev de HEXA; se sobreescribe en despliegues reales
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        return services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // conserva los nombres crudos: sub, company_id, role
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = false,   // los tokens de HEXA no llevan iss/aud
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

                        // company_id → tenant_id (lo lee TenantResolutionMiddleware)
                        var companyId = identity.FindFirst("company_id")?.Value;
                        if (!string.IsNullOrWhiteSpace(companyId) && identity.FindFirst(TenantIdClaim) is null)
                        {
                            identity.AddClaim(new Claim(TenantIdClaim, companyId));
                        }

                        // role de HEXA → scopes Nexo (HEXA no trae claim de scope)
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
