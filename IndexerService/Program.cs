using Microsoft.EntityFrameworkCore;
using SharedLibrary;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<DbContextConfig>(options =>
    options.UseNpgsql());

var app = builder.Build();
app.Run();