using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using SharedLibrary;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//  Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341") 
    .Enrich.FromLogContext()
    .CreateLogger();

Log.Information(" Starting SearchAPI...");

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

using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DbContextConfig>();
    db.Database.Migrate();
}

Log.Information(" Database connection configured successfully!");

//  Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(traceBuilder =>
    {
        traceBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SearchAPI"))
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
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SearchAPI"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });

Log.Information("OpenTelemetry configured!");

//  Add API controllers
builder.Services.AddControllers();

//  Setup Swagger (OpenAPI)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Search API",
        Version = "v1",
        Description = "API for searching cleaned emails in PostgreSQL"
    });
});
Log.Information(" Swagger (OpenAPI) configured!");

//  Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin",
        builder => builder
            .WithOrigins("https://trustedwebsite.com", "https://anothertrustedwebsite.com")
            .AllowAnyMethod()
            .AllowAnyHeader());
});
Log.Information("CORS policy applied!");

// Configure SPA frontend path
var frontEndRelativePath = "./../web-ui/www/";
builder.Services.AddSpaStaticFiles(configuration => 
{
    configuration.RootPath = frontEndRelativePath;
});
Log.Information(" SPA Static Files set to {Path}", frontEndRelativePath);

// Configure Serilog for application
builder.Host.UseSerilog();

// Configure the application to listen on http://localhost:5000
builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

// Enable Swagger UI in development mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Search API V1");
        c.RoutePrefix = "swagger";
    });
    Log.Information(" Swagger UI enabled at /swagger");
}

//  Enable CORS
app.UseCors(options =>
{
    options.SetIsOriginAllowed(origin => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
});
Log.Information(" CORS settings applied!");

//  Serve Static SPA Files
app.UseSpaStaticFiles(new StaticFileOptions()
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24;
        ctx.Context.Response.Headers[HeaderNames.CacheControl] =
            "public,max-age=" + durationInSeconds;
    }
});
Log.Information(" Static files caching enabled!");

//  Setup frontend routing for SPA
app.Map("/web-ui",
    (IApplicationBuilder frontendApp) =>
    {
        frontendApp.UseSpa(spa =>
        {
            spa.Options.SourcePath = "./app/www/";
        });
    });
Log.Information(" SPA frontend configured!");

//  Enable API controllers
app.UseRouting();
app.UseHttpMetrics(); 
app.MapControllers();
app.MapMetrics();

//  Log application startup
Log.Information(" {ServiceName} is now running...", app.Environment.ApplicationName);

//  Graceful shutdown handling
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information(" Shutting down SearchAPI...");
    Log.CloseAndFlush();
});

app.Run();
