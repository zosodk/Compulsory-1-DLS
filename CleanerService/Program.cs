using CleanerService.Services;
using DotNetEnv;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//  Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341") 
    .Enrich.FromLogContext()
    .CreateLogger();


//  Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(traceBuilder =>
    {
        traceBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CleanerMicroservice"))
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
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CleanerMicroservice"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });

Log.Information(" OpenTelemetry configured!");


//  Load environment variables
Env.Load();
Log.Information(" .env loaded successfully!");


//  Get paths for mail processing
string basePath = Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;
string inputFolder = Path.Combine(basePath, "maildir");  
string outputFolder = Path.Combine(basePath, "cleaned_mails"); 


//  Ensure cleaned_mails folder exists
if (!Directory.Exists(outputFolder))
{
    Directory.CreateDirectory(outputFolder);
    Log.Information(" Created output folder: {outputFolder}", outputFolder);
}

//  Get RabbitMQ host
string rabbitMqHost = Env.GetString("RABBITMQ_HOST", "localhost");
Log.Information(" Using RabbitMQ host: {rabbitMqHost}", rabbitMqHost);


//  Initialize Cleaner Service and Process Files
try
{
    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<MailCleaner>>();
    var cleaner = new MailCleaner(inputFolder, outputFolder, rabbitMqHost, logger);

    Log.Information("Processing files from {inputFolder}", inputFolder);
    cleaner.ProcessFiles();
    
    Log.Information(" CleanerService finished processing.");
}
catch (Exception ex)
{
    Log.Error(" Error processing files: {Message}", ex.Message);
}

//  Add controllers
builder.Services.AddControllers();

// Configure Serilog for application
builder.Host.UseSerilog();

var app = builder.Build();
app.UseRouting();
app.MapControllers();

//  Log application startup
Log.Information(" {ServiceName} is now running...", app.Environment.ApplicationName);

// Shutdown 
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information(" Shutting down CleanerService...");
    Log.CloseAndFlush();
});

app.Run();
