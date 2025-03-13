using DotNetEnv;
using IndexerService.Services;
using IndexerService.Shards;
using Microsoft.EntityFrameworkCore;
using SharedLibrary;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Prometheus;
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
var mainDbConnectionString = $"Host={Env.GetString("DB_HOST")};" +
                             $"Database={Env.GetString("DB_DATABASE")};" +
                             $"Username={Env.GetString("DB_USER")};" +
                             $"Password={Env.GetString("DB_PASSWORD")};" +
                             $"Port={Env.GetString("DB_PORT")};" +
                             $"SSL Mode=Require;";

string shard1DbConnectionString = $"Host={Env.GetString("DB_SHARD1_HOST")};" +
                                  $"Database={Env.GetString("DB_SHARD1_NAME")};" +
                                  $"Username={Env.GetString("DB_SHARD1_USER")};" +
                                  $"Password={Env.GetString("DB_SHARD1_PASSWORD")};" +
                                  $"Port={Env.GetString("DB_SHARD1_PORT")};" +
                                  $"SSL Mode=Require;";

string shard2DbConnectionString = $"Host={Env.GetString("DB_SHARD2_HOST")};" +
                                  $"Database={Env.GetString("DB_SHARD2_NAME")};" +
                                  $"Username={Env.GetString("DB_SHARD2_USER")};" +
                                  $"Password={Env.GetString("DB_SHARD2_PASSWORD")};" +
                                  $"Port={Env.GetString("DB_SHARD2_PORT")};" +
                                  $"SSL Mode=Require;";

Log.Information(" Using PostgreSQL at {Host}:{Port}, Database: {Database}", 
                Env.GetString("DB_HOST"), 
                Env.GetString("DB_PORT"), 
                Env.GetString("DB_DATABASE"));

// Register main database and shards
builder.Services.AddDbContext<DbContextConfig>(options =>
    options.UseNpgsql(mainDbConnectionString));

builder.Services.AddSingleton(provider => 
    new ShardedDbContext(new List<string>
{
    shard1DbConnectionString,
    shard2DbConnectionString
}));

Log.Information(" Database connections configured successfully!");

// Run migrations
using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DbContextConfig>();
    db.Database.Migrate();
}


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
app.UseHttpMetrics();
app.MapMetrics();

//  Log application startup
Log.Information(" {ServiceName} is now running...", app.Environment.ApplicationName);


app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information(" Shutting down IndexerService...");
    Log.CloseAndFlush();
});

app.Run();
