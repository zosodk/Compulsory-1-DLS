using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using SharedLibrary;

var builder = WebApplication.CreateBuilder(args);

string envPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, ".env");
Env.Load(envPath);


var connectionString = $"Host={Env.GetString("DB_HOST")};" +
                       $"Database={Env.GetString("DB_DATABASE")};" +
                       $"Username={Env.GetString("DB_USER")};" +
                       $"Password={Env.GetString("DB_PASSWORD")};" +
                       $"Port={Env.GetString("DB_PORT")};" +
                       $"SSL Mode=Require;";


Console.WriteLine("PostgreSQL Connection String: " + connectionString);


builder.Services.AddDbContext<DbContextConfig>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddControllers();

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


var frontEndRelativePath = "./../web-ui/www/";

builder.Services.AddSpaStaticFiles(configuration => 
    { configuration.RootPath = "./../web-ui/www/"; });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin",
        builder => builder
            .WithOrigins("https://trustedwebsite.com", "https://anothertrustedwebsite.com") 
            .AllowAnyMethod()
            .AllowAnyHeader());
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Search API V1");
        c.RoutePrefix = "swagger"; 
    });
}


app.UseCors(options =>
{
    options.SetIsOriginAllowed(origin => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
});

app.UseSpaStaticFiles(new StaticFileOptions()
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24;
        ctx.Context.Response.Headers[HeaderNames.CacheControl] =
            "public,max-age=" + durationInSeconds;
    }
});

app.Map("/web-ui",
    (IApplicationBuilder frontendApp) =>
    { frontendApp.UseSpa(spa =>
        { spa.Options.SourcePath = "./app/www/"; }); });


app.UseSpaStaticFiles();
app.UseSpa(conf =>
{
    conf.Options.SourcePath = frontEndRelativePath;
});

app.UseRouting();
app.MapControllers();
app.Run();