using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Nexo.BuildingBlocks.MultiTenancy;
using Nexo.BuildingBlocks.Observability;
using Nexo.BuildingBlocks.Web;
using Nexo.EventGateway;

var builder = WebApplication.CreateBuilder(args);

builder.AddNexoObservability("nexo-event-gateway");
builder.Services.AddNexoWeb();
builder.Services.AddMultiTenancy(builder.Configuration);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<DeliveryService>();

if (builder.Environment.IsDevelopment())
{
    if (string.Equals(builder.Configuration["Auth:Mode"], "HexaJwt", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddNexoHexaJwt(builder.Configuration);
    }
    else
    {
        builder.Services.AddNexoDevAuth();
    }

    builder.Services.AddNexoDevCors();
}
else
{
    builder.Services.AddNexoHexaJwt(builder.Configuration);
}

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexo MES · Event Gateway", Version = "v1" });
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
    app.UseCors(DevCors.PolicyName);
}

app.UseAuthentication();
app.UseNexoWeb();
app.UseAuthorization();

app.MapEventGatewayEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();
