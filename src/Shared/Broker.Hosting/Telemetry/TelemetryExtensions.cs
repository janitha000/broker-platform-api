using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Broker.Hosting.Telemetry;

public static class TelemetryExtensions
{
    public static IServiceCollection AddBrokerTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string? serviceName)
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        var name = FirstNonEmpty(
            serviceName,
            configuration["Telemetry:ServiceName"],
            configuration["OTEL_SERVICE_NAME"]) ?? "broker-api";

        var otlp = FirstNonEmpty(
            configuration["Telemetry:OtlpEndpoint"],
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(name))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(BrokerTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlp))
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlp));
            });

        return services;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
}
