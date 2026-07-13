using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Nexo.BuildingBlocks.Observability;

/// <summary>Wires up Serilog logging and OpenTelemetry tracing/metrics for a Nexo service.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Configures structured logging (Serilog) and OpenTelemetry (ASP.NET Core + runtime
    /// instrumentation, exported over OTLP to <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>).
    /// </summary>
    public static IHostApplicationBuilder AddNexoObservability(
        this IHostApplicationBuilder builder,
        string serviceName)
    {
        // Serilog. LogContext lets middleware enrich entries with tenant_id / correlation_id
        // when those properties are pushed onto the context for the current request.
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("service.name", serviceName)
            .WriteTo.Console()
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(options => ConfigureOtlpEndpoint(options, otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(options => ConfigureOtlpEndpoint(options, otlpEndpoint)));

        return builder;
    }

    private static void ConfigureOtlpEndpoint(OtlpExporterOptions options, string? endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            options.Endpoint = new Uri(endpoint);
        }
    }
}
