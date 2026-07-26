using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Messaging;
using Nexo.BuildingBlocks.Outbox;
using Nexo.BuildingBlocks.MultiTenancy;
using Nexo.BuildingBlocks.Observability;
using Nexo.BuildingBlocks.Web;
using Nexo.Execution.Api;
using Nexo.Execution.Application;
using Nexo.Execution.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Observability: Serilog + OpenTelemetry (traces/metrics, OTLP exporter) ---
builder.AddNexoObservability("nexo-execution");

// --- Web building blocks: exception handling (RFC7807) + tenant resolution middleware ---
builder.Services.AddNexoWeb();

// --- Multi-tenancy: ITenantContext (scoped) + ConfigurationTenantConnectionResolver (Tenants section) ---
builder.Services.AddMultiTenancy(builder.Configuration);

// --- Application (MediatR + pipeline behaviors) for the Execution Application assembly ---
builder.Services.AddApplication(typeof(CreateExecutionCommand).Assembly);

// The ValidationBehavior resolves IValidator<TRequest> from the container, so the FluentValidation
// validators of the Application assembly have to be registered.
builder.Services.AddValidatorsFromAssemblyContaining<CreateExecutionCommandValidator>();

// --- Infrastructure: per-tenant ExecutionDbContext (connection resolved at request time) ---
builder.Services.AddExecutionInfrastructure();

// --- Outbox relay (M1): drains execution.outbox_messages to Kafka every couple of seconds ---
builder.Services.AddOutboxRelay<ExecutionDbContext>();

// --- Messaging: MassTransit control bus + Kafka rider with the domain producers ---
// NOTE: publication is via the transactional outbox (execution.outbox_messages, written in
// ExecutionDbContext.SaveChanges). Wiring the outbox relay/drainer to these producers is a TODO
// (MassTransit EF outbox or a hosted relay — see docs/design/02-event-model.md §5.1 / DT-EV-02).
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));

    x.AddRider(rider =>
    {
        rider.AddProducer<ExecutionCreatedIntegrationEvent>($"{EventTypes.Execution_Created}.v1");
        rider.AddProducer<ExecutionStartedIntegrationEvent>($"{EventTypes.Execution_Started}.v1");
        rider.AddProducer<ExecutionClosedIntegrationEvent>($"{EventTypes.Execution_Closed}.v1");
        rider.AddProducer<ExecutionCancelledIntegrationEvent>($"{EventTypes.Execution_Cancelled}.v1");
        rider.AddProducer<ExecutionInputConsumedIntegrationEvent>($"{EventTypes.Execution_InputConsumed}.v1");
        rider.AddProducer<ExecutionMilestoneReachedIntegrationEvent>($"{EventTypes.Execution_MilestoneReached}.v1");
        rider.AddProducer<TaskEnabledIntegrationEvent>($"{EventTypes.Task_Enabled}.v1");
        rider.AddProducer<TaskAssignedIntegrationEvent>($"{EventTypes.Task_Assigned}.v1");
        rider.AddProducer<TaskStartedIntegrationEvent>($"{EventTypes.Task_Started}.v1");
        rider.AddProducer<TaskProgressReportedIntegrationEvent>($"{EventTypes.Task_ProgressReported}.v1");
        rider.AddProducer<TaskBlockedIntegrationEvent>($"{EventTypes.Task_Blocked}.v1");
        rider.AddProducer<TaskUnblockedIntegrationEvent>($"{EventTypes.Task_Unblocked}.v1");
        rider.AddProducer<TaskCompletedIntegrationEvent>($"{EventTypes.Task_Completed}.v1");
        rider.AddProducer<TaskSkippedIntegrationEvent>($"{EventTypes.Task_Skipped}.v1");
        rider.AddProducer<TaskEvidenceAttachedIntegrationEvent>($"{EventTypes.Task_EvidenceAttached}.v1");

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

// --- AuthZ: scope policies per endpoint (nexo.execution.read / .write) ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("execution.read", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx => HasScope(ctx, "nexo.execution.read")));

    options.AddPolicy("execution.write", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx => HasScope(ctx, "nexo.execution.write")));
});

// --- OpenAPI / Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexo Execution API", Version = "v1" });

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

app.MapExecutionEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();

static bool HasScope(AuthorizationHandlerContext context, string requiredScope)
    => context.User.Claims
        .Where(claim => claim.Type is "scope" or "scp")
        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Contains(requiredScope);
