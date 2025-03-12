using CleanerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter.OpenTelemetryProtocol;
using OpenTelemetry.Metrics; // Add using for metrics
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using SharedLibrary.Settings;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext())
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<RabbitMQSettings>(hostContext.Configuration.GetSection("RabbitMQ"));
        services.Configure<OtlpSettings>(hostContext.Configuration.GetSection("Otlp"));
        services.Configure<MaildirSettings>(hostContext.Configuration.GetSection("Maildir"));

        services.AddOpenTelemetry() // Updated to AddOpenTelemetry
            .ConfigureResource(resourceBuilder =>
            {
                resourceBuilder.AddService("CleanerService");
            })
            .WithTracing(tracingBuilder => // Configure tracing
            {
                tracingBuilder
                    .AddSource("CleanerService")
                    .AddOtlpExporter(otlpOptions =>
                    {
                        var otlpSettings = hostContext.Configuration.GetSection("Otlp").Get<OtlpSettings>();
                        otlpOptions.Endpoint = new Uri(otlpSettings.Endpoint);
                    })
                    .AddAspNetCoreInstrumentation();
            })
            .WithMetrics(metricsBuilder => // Configure metrics
            {
                // Add metrics instrumentation if needed
            });

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();