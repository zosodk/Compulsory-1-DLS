using Microsoft.EntityFrameworkCore;
using SharedLibrary;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DbContextConfig>(options =>
    options.UseNpgsql(),
    ServiceLifetime.Scoped);

var app = builder.Build();
app.Run();