using Application;
using Infrastructure;
using Scalar.AspNetCore;
using DotNetEnv;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Domain.Entities;
using Application.Common.Settings;
using Application.Common.Security;
using Infrastructure.Services.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer; 
using Microsoft.IdentityModel.Tokens; 
using System.Text;
using Serilog;
using Serilog.Events;
using Prometheus;

Env.Load();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/nexusfs-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Starting NexusFS API");

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<Infrastructure.Services.ProviderManager>();

// Health Checks
var healthChecksBuilder = builder.Services.AddHealthChecks();

// Add PostgreSQL health check - prioritize DATABASE_URL environment variable
var postgresConnection = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(postgresConnection))
{
    healthChecksBuilder.AddNpgSql(postgresConnection);
    Log.Information("PostgreSQL health check configured");
}
else
{
    Log.Warning("PostgreSQL connection string not configured - health check skipped");
}

// Add Redis health check - prioritize REDIS_URL environment variable
var redisConnection = Environment.GetEnvironmentVariable("REDIS_URL") 
    ?? builder.Configuration.GetSection("Redis:ConnectionString").Get<string>();

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    healthChecksBuilder.AddRedis(redisConnection);
    Log.Information("Redis health check configured with: {RedisConnection}", redisConnection);
}
else
{
    Log.Warning("Redis connection string not configured - health check skipped");
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                 "http://localhost:5173",
                 "https://fe-nexus-fs-elizadoltu-eliza-doltus-projects.vercel.app",
                 "https://fe-nexus-fs.vercel.app"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddTransient<DatabaseSeeder>();
builder.Services.AddScoped<IPasswordHasher<UserEntity>, PasswordHasher<UserEntity>>();

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    throw new InvalidOperationException("JWT Settings are not configured properly.");
}

var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; 
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero, 
        
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var seeder = services.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();

    try
    {
        //we can check if the database exists also here..?
        var providerManager = services.GetRequiredService<Infrastructure.Services.ProviderManager>();
        await providerManager.LoadProvidersFromDatabaseAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<Infrastructure.Services.Observability.Logger>();
        logger.LogError("An error occurred while loading providers into memory.");
        logger.LogError(ex.Message);
    }

}

Console.WriteLine("Admin username (config): " + builder.Configuration["Seed:Admin:Username"]);

app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options
        .WithTitle("NexusFS API Documentation")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// Prometheus metrics
app.UseHttpMetrics();
app.MapMetrics();

// Health Checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

app.UseAuthentication(); 
app.UseAuthorization(); 

app.MapControllers();

app.MapGet("/weatherforecast", () =>
{
    var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

    Log.Information("NexusFS API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NexusFS API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}