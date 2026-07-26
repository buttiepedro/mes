using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nexo.BuildingBlocks.Web;

/// <summary>
/// Development-only authentication that bypasses the real IdP (Duende IdentityServer, not yet built).
/// It authenticates <b>every</b> request as a fixed dev user carrying all Nexo scopes and the demo
/// tenant, so the APIs can be exercised end-to-end from Swagger/cURL without a token.
/// </summary>
/// <remarks>
/// This is development scaffolding, NOT the security model. It must only ever be registered when the
/// host environment is Development (see <see cref="DevAuthentication.AddNexoDevAuth"/>); the real
/// JWT/Duende flow stands in every other environment. Scopes and tenant can be overridden via the
/// <c>DevAuth:Scopes</c> / <c>DevAuth:TenantId</c> configuration keys.
/// </remarks>
public sealed class DevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Name of the development bypass authentication scheme.</summary>
    public const string SchemeName = "DevBypass";

    // All Nexo API scopes: the dev user satisfies every service's read/write authorization policy.
    private const string DefaultScopes =
        "nexo.masterdata.read nexo.masterdata.write " +
        "nexo.workmodel.read nexo.workmodel.write " +
        "nexo.execution.read nexo.execution.write " +
        "nexo.production.read nexo.production.write";

    // Matches the "demo" tenant in each service's appsettings.Development.json "Tenants" section.
    private const string DefaultTenantId = "9c3b1e77-2d4a-4b8f-9e1a-6f0c2d3b4a55";

    private readonly IConfiguration _configuration;

    public DevAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var scopes = _configuration["DevAuth:Scopes"] ?? DefaultScopes;
        var tenantId = _configuration["DevAuth:TenantId"] ?? DefaultTenantId;

        var claims = new[]
        {
            new Claim("sub", "dev-user"),
            new Claim("name", "Nexo Dev"),
            new Claim("scope", scopes),
            new Claim("tenant_id", tenantId),
        };

        var identity = new ClaimsIdentity(claims, SchemeName, nameType: "name", roleType: "role");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>Registration helper for the development authentication bypass.</summary>
public static class DevAuthentication
{
    /// <summary>
    /// Registers the <see cref="DevAuthenticationHandler"/> as the default authentication scheme,
    /// bypassing JWT/Duende. Call this INSTEAD of the real <c>AddJwtBearer</c> registration, and only
    /// when <c>env.IsDevelopment()</c> is true.
    /// </summary>
    public static AuthenticationBuilder AddNexoDevAuth(this IServiceCollection services)
        => services
            .AddAuthentication(DevAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(
                DevAuthenticationHandler.SchemeName, static _ => { });
}
