using DotNetEnv;
using IndexerService.Services;
using IndexerService.Shards;
using Microsoft.EntityFrameworkCore;
using SharedLibrary;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog 
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341") 
    .Enrich.FromLogContext()
    .CreateLogger();

Log.Information(" Starting IndexerService...");

//  Load environment variables
string envPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, ".env");
Env.Load(envPath);

Log.Information(" .env loaded successfully!");

//  Build PostgreSQL connection string
var connectionString = $"Host={Env.GetString("DB_HOST")};" +
                       $"Database={Env.GetString("DB_DATABASE")};" +
                       $"Username={Env.GetString("DB_USER")};" +
                       $"Password={Env.GetString("DB_PASSWORD")};" +
                       $"Port={Env.GetString("DB_PORT")};" +
                       $"SSL Mode=Require;";

Log.Information(" Using PostgreSQL at {Host}:{Port}, Database: {Database}", 
                Env.GetString("DB_HOST"), 
                Env.GetString("DB_PORT"), 
                Env.GetString("DB_DATABASE"));

//  Register database connection
builder.Services.AddDbContext<DbContextConfig>(options =>
    options.UseNpgsql(connectionString));

Log.Information(" Database connection configured successfully!");

//  Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(traceBuilder =>
    {
        traceBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("IndexerMicroservice"))
            .AddAspNetCoreInstrumentation() 
            .AddHttpClientInstrumentation() 
            .AddZipkinExporter(options =>
            {
                options.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
            });
    })
    .WithMetrics(metricBuilder =>
    {
        metricBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("IndexerMicroservice"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });

Log.Information(" OpenTelemetry configured!");

//  Setup RabbitMQ
builder.Services.AddSingleton<RabbitMQConfig>();
builder.Services.AddScoped<MailIndexer>(); 
builder.Services.AddHostedService<WorkerService>();
builder.Services.AddSingleton<ShardedDbContext>();

Log.Information(" RabbitMQ services registered!");

//  Configure Serilog 
builder.Host.UseSerilog();

var app = builder.Build();
app.UseRouting();

//  Log application startup
Log.Information(" {ServiceName} is now running...", app.Environment.ApplicationName);


app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information(" Shutting down IndexerService...");
    Log.CloseAndFlush();
});

app.Run();
