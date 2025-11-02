using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Infrastructure.Services.Security;
using Infrastructure.Services.UI;
using Infrastructure.Configuration;
using Domain.Repositories;
using Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Clerk.Net;

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

        // Observability (Scoped)
        services.AddScoped<Logger>();
        services.AddScoped<MetricsCollector>();
        services.AddScoped<IProviderObserver, MetricsCollector>();
        services.AddScoped<IProviderObserver, Logger>();

        // Security
        services.AddScoped<ACLManager>();
        services.AddScoped<AuthManager>();
        
        // Clerk Services - simplified for now
        services.AddScoped<ClerkAuthStrategy>();
        services.AddScoped<IClerkAuthService, ClerkAuthService>();
        
        // Clerk User Management Service
        services.AddScoped<IClerkUserService, ClerkUserService>();
        
        // Hybrid User Service (combines DB and Clerk users)
        services.AddScoped<HybridUserService>();

        // UI Services
        services.AddScoped<ProviderUIService>();

        return services;
    }
}