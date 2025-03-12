using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using SharedLibrary;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DbContextConfig>(options =>
    options.UseNpgsql(),
    ServiceLifetime.Scoped);
builder.Services.AddControllers();

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


app.UseSwagger();
app.UseSwaggerUI();

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


app.MapControllers();

app.Run();