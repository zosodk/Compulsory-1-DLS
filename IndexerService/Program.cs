
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
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

var app = builder.Build();
app.Run();
