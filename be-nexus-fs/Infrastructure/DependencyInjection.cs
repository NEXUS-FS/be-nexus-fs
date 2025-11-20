using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Infrastructure.Services.Security;
using Infrastructure.Services.UI;
using Infrastructure.Services.FileOperations;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
                               ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string not found. Set DATABASE_URL in .env file or ConnectionStrings:DefaultConnection in appsettings.json");
        }
        // Database
        services.AddDbContext<NexusFSDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions => 
                npgsqlOptions.EnableRetryOnFailure()));

        // Repositories
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccessControlRepository, AccessControlRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IMetricRepository, MetricRepository>();

        // Factory (Singleton - no dependencies)
        services.AddSingleton<ProviderFactory>();

        // Core Services
        services.AddScoped<ProviderManager>();
        services.AddScoped<ProviderRouter>();
        services.AddScoped<IFileOperationRepository, FileOperationRepository>();

        // Observability (Scoped)
        services.AddScoped<Logger>();
        services.AddScoped<MetricsCollector>();
        services.AddScoped<IProviderObserver, MetricsCollector>();
        services.AddScoped<IProviderObserver, Logger>();

        // Security
        services.AddScoped<ACLManager>();
        services.AddScoped<AuthManager>();

        // UI Services
        services.AddScoped<ProviderUIService>();

        return services;
    }
}