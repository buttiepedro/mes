using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.MultiTenancy;
using Nexo.BuildingBlocks.Observability;
using Nexo.BuildingBlocks.Web;
using Nexo.Production.Api;
using Nexo.Production.Application;
using Nexo.Production.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Observability: Serilog + OpenTelemetry (traces/metrics, OTLP exporter) ---
builder.AddNexoObservability("nexo-production");

// --- Web building blocks: exception handling (RFC7807) + tenant resolution middleware ---
builder.Services.AddNexoWeb();

// --- Multi-tenancy: ITenantContext (scoped) + ConfigurationTenantConnectionResolver (Tenants section) ---
builder.Services.AddMultiTenancy(builder.Configuration);

// --- Application (MediatR + pipeline behaviors) for the Production Application assembly ---
builder.Services.AddApplication(typeof(RegisterProductionCommand).Assembly);

// --- Infrastructure: per-tenant ProductionDbContext (connection resolved at request time) ---
builder.Services.AddProductionInfrastructure();

// --- Messaging: MassTransit control bus + Kafka rider with the domain producers ---
// NOTE: publication is via the transactional outbox (platform.outbox_messages, written in
// ProductionDbContext.SaveChanges). Wiring the outbox relay/drainer to these producers is a TODO
// (MassTransit EF outbox or a hosted relay — see docs/design/02-event-model.md §5.1 / DT-EV-02).
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));

    x.AddRider(rider =>
    {
        rider.AddProducer<ProductionRegisteredIntegrationEvent>("nexo.production.registered.v1");
        rider.AddProducer<RunClosedIntegrationEvent>("nexo.production.run_closed.v1");

        rider.UsingKafka((context, k) =>
        {
            k.Host(builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092");
        });
    });
});

// --- AuthN: JWT bearer validated locally via JWKS from Duende (Authority), audience nexo.api ---
// M0 (MVP execution roadmap): in Development the Duende IdP is not running yet, so we bypass it with
// a dev-only scheme that authenticates every request as a dev user with all scopes + the demo tenant.
// The real JWT/Duende flow stands in every other environment.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddNexoDevAuth();
}
else
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Authority:Issuer"];
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Authority:Audience"] ?? "nexo.api"
            };
        });
}

// --- AuthZ: scope policies per endpoint (nexo.production.read / .write) ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("production.read", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx => HasScope(ctx, "nexo.production.read")));

    options.AddPolicy("production.write", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx => HasScope(ctx, "nexo.production.write")));
});

// --- OpenAPI / Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexo Production API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

// --- Health checks ---
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseNexoWeb();          // exception handling + tenant resolution (reads tenant_id claim / X-Tenant-Key)
app.UseAuthorization();

app.MapProductionEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();

static bool HasScope(AuthorizationHandlerContext context, string requiredScope)
    => context.User.Claims
        .Where(claim => claim.Type is "scope" or "scp")
        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Contains(requiredScope);
