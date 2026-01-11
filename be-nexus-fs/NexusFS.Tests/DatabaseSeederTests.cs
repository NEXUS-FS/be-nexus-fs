using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Services.Observability;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace NexusFS.Tests
{
    public class DatabaseSeederTests
    {
        [Fact]
        public async Task SeedAsync_WhenUsersExist_DoesNotSeed()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<NexusFSDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_UsersExist")
                .Options;

            using var context = new NexusFSDbContext(options);
            var existingUser = new UserEntity { Id = "1", Username = "existing", Email = "existing@test.com", Role = "User", Provider = "Basic", IsActive = true };
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var auditLogRepoMock = new Mock<IAuditLogRepository>();
            var loggerMock = new Mock<Logger>(auditLogRepoMock.Object);
            var config = new ConfigurationBuilder().Build(); // Empty config for first test
            var passwordHasher = new PasswordHasher<UserEntity>();

            var seeder = new DatabaseSeeder(context, loggerMock.Object, config, passwordHasher);

            // Act
            await seeder.SeedAsync();

            // Assert
            context.Users.Count().Should().Be(1); // Only the existing user
        }

        [Fact]
        public async Task SeedAsync_WhenNoUsersExist_CreatesDefaultAdmin()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<NexusFSDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_NoUsers")
                .Options;

            using var context = new NexusFSDbContext(options);

            var auditLogRepoMock = new Mock<IAuditLogRepository>();
            var loggerMock = new Mock<Logger>(auditLogRepoMock.Object);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Seed:Admin:Username", "TestAdmin"},
                    {"Seed:Admin:Password", "TestPass123"},
                    {"Seed:Admin:Email", "admin@test.com"}
                })
                .Build();
            var passwordHasher = new PasswordHasher<UserEntity>();

            var seeder = new DatabaseSeeder(context, loggerMock.Object, config, passwordHasher);

            // Act
            await seeder.SeedAsync();

            // Assert
            context.Users.Count().Should().Be(1);
            var adminUser = context.Users.First();
            adminUser.Username.Should().Be("TestAdmin");
            adminUser.Email.Should().Be("admin@test.com");
            adminUser.Role.Should().Be("Admin");
            adminUser.Provider.Should().Be("Basic");
            adminUser.IsActive.Should().BeTrue();
            adminUser.PasswordHash.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task SeedAsync_UsesDefaultValuesWhenConfigIsNull()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<NexusFSDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_Defaults")
                .Options;

            using var context = new NexusFSDbContext(options);

            var auditLogRepoMock = new Mock<IAuditLogRepository>();
            var loggerMock = new Mock<Logger>(auditLogRepoMock.Object);
            var config = new ConfigurationBuilder().Build(); // Empty config, should use defaults
            var passwordHasher = new PasswordHasher<UserEntity>();

            var seeder = new DatabaseSeeder(context, loggerMock.Object, config, passwordHasher);

            // Act
            await seeder.SeedAsync();

            // Assert
            context.Users.Count().Should().Be(1);
            var adminUser = context.Users.First();
            adminUser.Username.Should().Be("Admin");
            adminUser.Email.Should().Be("admin@example.com");
            adminUser.PasswordHash.Should().NotBeNullOrEmpty();
        }
    }
}