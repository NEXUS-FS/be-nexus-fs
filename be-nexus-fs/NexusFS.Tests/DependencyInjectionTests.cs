using System;
using FluentAssertions;
using Infrastructure;
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
using Xunit;

namespace NexusFS.Tests
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void AddInfrastructure_RegistersAllServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test;Username=test;Password=test"),
                    new KeyValuePair<string, string?>("Redis:ConnectionString", "localhost:6379"),
                    new KeyValuePair<string, string?>("Redis:InstanceName", "Test:")
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Assert - Build service provider
            var serviceProvider = services.BuildServiceProvider();

            // Test repositories
            serviceProvider.GetService<IProviderRepository>().Should().NotBeNull();
            serviceProvider.GetService<IUserRepository>().Should().NotBeNull();
            serviceProvider.GetService<IAccessControlRepository>().Should().NotBeNull();
            serviceProvider.GetService<IAuditLogRepository>().Should().NotBeNull();
            serviceProvider.GetService<IMetricRepository>().Should().NotBeNull();

            // Test factory
            serviceProvider.GetService<ProviderFactory>().Should().NotBeNull();

            // Test core services
            serviceProvider.GetService<ProviderManager>().Should().NotBeNull();
            serviceProvider.GetService<ProviderRouter>().Should().NotBeNull();
            serviceProvider.GetService<UriRouter>().Should().NotBeNull();
            serviceProvider.GetService<IFileOperationRepository>().Should().NotBeNull();

            // Test observability
            serviceProvider.GetService<Logger>().Should().NotBeNull();
            serviceProvider.GetService<MetricsCollector>().Should().NotBeNull();

            // Test security
            serviceProvider.GetService<ACLManager>().Should().NotBeNull();
            serviceProvider.GetService<AuthManager>().Should().NotBeNull();

            // Test UI services
            serviceProvider.GetService<ProviderUIService>().Should().NotBeNull();

            // Test DbContext
            serviceProvider.GetService<NexusFSDbContext>().Should().NotBeNull();
        }

        [Fact]
        public void AddInfrastructure_WithoutDatabaseConnection_ThrowsException()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Act & Assert
            Action act = () => services.AddInfrastructure(configuration);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Database connection string not found. Set DATABASE_URL in .env file or ConnectionStrings:DefaultConnection in appsettings.json");
        }

        [Fact]
        public void AddInfrastructure_WithEnvironmentVariable_UsesEnvironmentVariable()
        {
            // Arrange
            var originalValue = Environment.GetEnvironmentVariable("DATABASE_URL");
            try
            {
                Environment.SetEnvironmentVariable("DATABASE_URL", "Host=envhost;Database=envdb;Username=envuser;Password=envpass");
                var services = new ServiceCollection();
                var configuration = new ConfigurationBuilder().Build();

                // Act
                services.AddInfrastructure(configuration);

                // Assert
                var serviceProvider = services.BuildServiceProvider();
                serviceProvider.GetService<NexusFSDbContext>().Should().NotBeNull();
            }
            finally
            {
                Environment.SetEnvironmentVariable("DATABASE_URL", originalValue);
            }
        }
    }
}