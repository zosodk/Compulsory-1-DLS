using CleanerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter.OpenTelemetryProtocol;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using WorkerShares.Settings;
using WorkerShares.Interfaces;
using RabbitMQ.Client;

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

        services.AddOpenTelemetry()
            .ConfigureResource(resourceBuilder =>
            {
                resourceBuilder.AddService("CleanerService");
            })
            .WithTracing(tracingBuilder =>
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
            .WithMetrics(metricsBuilder =>
            {
                // Add metrics instrumentation if needed
            });

        // Add RabbitMQ interface registrations
        services.AddSingleton<IConnectionWrapper>(sp =>
        {
            var rabbitSettings = sp.GetRequiredService<IOptions<RabbitMQSettings>>().Value;
            var factory = new ConnectionFactory() { HostName = rabbitSettings.HostName };
            IConnection connection = (IConnection)factory.CreateConnectionAsync(); // This is correct.
            return new ConnectionWrapper(connection);
        });

        services.AddSingleton<IModelWrapper>(sp =>
        {
            var connectionWrapper = sp.GetRequiredService<IConnectionWrapper>();
            var model = connectionWrapper.CreateModel();
            return new ModelWrapper(model);
        });

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();