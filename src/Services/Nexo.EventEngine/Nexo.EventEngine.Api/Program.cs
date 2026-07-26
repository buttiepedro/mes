using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nexo.BuildingBlocks.MultiTenancy;
using Nexo.BuildingBlocks.Observability;
using Nexo.BuildingBlocks.Web;
using Nexo.EventEngine.Api;

var builder = WebApplication.CreateBuilder(args);

// --- Observability: Serilog + OpenTelemetry ---
builder.AddNexoObservability("nexo-event-engine");

// --- Web building blocks: exception handling (RFC7807) + tenant resolution middleware ---
builder.Services.AddNexoWeb();
builder.Services.AddMultiTenancy(builder.Configuration);

// --- Capa 4 (event engine): in-memory progress read model + Kafka consumer that projects into it ---
builder.Services.AddSingleton<ExecutionProgressProjection>();
builder.Services.AddHostedService<ExecutionEventsConsumer>();

// --- AuthN: dev bypass in Development (M0), real JWT/Duende elsewhere ---
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

builder.Services.AddAuthorization();

// --- OpenAPI / Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexo Event Engine API", Version = "v1" });

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

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Static live dashboard (wwwroot/index.html) served before auth so the page itself is public; its
// fetch to /v1/executions/progress is authenticated (dev bypass in Development).
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseNexoWeb();          // exception handling + tenant resolution
app.UseAuthorization();

app.MapProgressEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();
