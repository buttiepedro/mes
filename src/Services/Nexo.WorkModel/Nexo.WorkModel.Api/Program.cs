using FluentValidation;
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
using Nexo.WorkModel.Api;
using Nexo.WorkModel.Application;
using Nexo.BuildingBlocks.Outbox;
using Nexo.WorkModel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Observability: Serilog + OpenTelemetry (traces/metrics, OTLP exporter) ---
builder.AddNexoObservability("nexo-workmodel");

// --- Web building blocks: exception handling (RFC7807) + tenant resolution middleware ---
builder.Services.AddNexoWeb();

// --- Multi-tenancy: ITenantContext (scoped) + ConfigurationTenantConnectionResolver (Tenants section) ---
builder.Services.AddMultiTenancy(builder.Configuration);

// --- Application (MediatR + pipeline behaviors) for the Work Model Application assembly ---
builder.Services.AddApplication(typeof(CreateProcessCommand).Assembly);

// The ValidationBehavior resolves IValidator<TRequest> from the container, so the FluentValidation
// validators of the Application assembly have to be registered.
builder.Services.AddValidatorsFromAssemblyContaining<CreateProcessCommandValidator>();

// --- Infrastructure: per-tenant WorkModelDbContext (connection resolved at request time) ---
builder.Services.AddWorkModelInfrastructure();

// --- Outbox relay (M1): drains work.outbox_messages to Kafka every couple of seconds ---
builder.Services.AddOutboxRelay<WorkModelDbContext>();

// --- Messaging: MassTransit control bus + Kafka rider with the domain producers ---
// NOTE: publication is via the transactional outbox (work.outbox_messages, written in
// WorkModelDbContext.SaveChanges). Wiring the outbox relay/drainer to these producers is a TODO
// (MassTransit EF outbox or a hosted relay — see docs/design/02-event-model.md §5.1 / DT-EV-02).
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));

    x.AddRider(rider =>
    {
        rider.AddProducer<ProcessVersionPublishedIntegrationEvent>("nexo.process.version_published.v1");
        rider.AddProducer<ProcessVersionSuspendedIntegrationEvent>("nexo.process.version_suspended.v1");

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

// --- AuthZ: scope policies per endpoint (nexo.workmodel.read / .write / .publish) ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("workmodel.read", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx => HasScope(ctx, "nexo.workmodel.read")));

    options.AddPolicy("workmodel.write", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx => HasScope(ctx, "nexo.workmodel.write")));

    // Publishing/suspending is a segregation-of-duties action: whoever writes a draft is not
    // necessarily allowed to make it executable (CB15).
    options.AddPolicy("workmodel.publish", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx => HasScope(ctx, "nexo.workmodel.publish")));
});

// --- OpenAPI / Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexo Work Model API", Version = "v1" });

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

app.MapWorkModelEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();

static bool HasScope(AuthorizationHandlerContext context, string requiredScope)
    => context.User.Claims
        .Where(claim => claim.Type is "scope" or "scp")
        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Contains(requiredScope);
