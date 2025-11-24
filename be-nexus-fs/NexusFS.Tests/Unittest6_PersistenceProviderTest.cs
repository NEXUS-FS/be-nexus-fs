using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

//Some tests to verify that ProviderManager works in its scope okay
namespace NexusFS.Tests
{
    public class ProviderManagerScopeTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ProviderManager _manager;
        private readonly NexusFSDbContext _dbContext;

        public ProviderManagerScopeTests()
        {
            Console.WriteLine("\n[Test Setup] Initializing DI Container...");
            var services = new ServiceCollection();

            // 1. Setup In-Memory DB with unique name per test class run
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<NexusFSDbContext>(opts => 
                opts.UseInMemoryDatabase(dbName));

            // 2. Register Dependencies
            services.AddScoped<IProviderRepository, ProviderRepository>();
            services.AddScoped<ProviderFactory>();
            
            var mockAuditRepo = new Mock<IAuditLogRepository>();
            services.AddSingleton(mockAuditRepo.Object);
            services.AddSingleton<Logger>();

            services.AddSingleton<ProviderManager>();

            _serviceProvider = services.BuildServiceProvider();
            _manager = _serviceProvider.GetRequiredService<ProviderManager>();
            _dbContext = _serviceProvider.GetRequiredService<NexusFSDbContext>();
            
            Console.WriteLine("[Test Setup] DI Container Ready.");
        }

        public void Dispose()
        {
            Console.WriteLine("[Test Teardown] Cleaning up DB...");
            _dbContext.Database.EnsureDeleted();
            _serviceProvider.Dispose();
        }

        [Fact]
        public async Task LoadProvidersFromDatabaseAsync_ShouldPopulateMemory_FromScopedRepo()
        {
            Console.WriteLine("\n>>> TEST START: LoadProvidersFromDatabaseAsync");

            // Arrange
            var validConfig = new Dictionary<string, string> { { "basePath", "C:/Temp/Startup" } };
            var jsonConfig = JsonSerializer.Serialize(validConfig);
            
            Console.WriteLine($"[Arrange] Seeding DB with Config: {jsonConfig}");

            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NexusFSDbContext>();
                db.Providers.Add(new ProviderEntity 
                { 
                    Id = "startup-provider", 
                    Name = "Startup", 
                    Type = "Local",
                    IsActive = true,
                    Configuration = jsonConfig 
                });
                await db.SaveChangesAsync();
                Console.WriteLine("[Arrange] Seed Saved to DB.");
            }

            // Act
            Console.WriteLine("[Act] Calling _manager.LoadProvidersFromDatabaseAsync()...");
            await _manager.LoadProvidersFromDatabaseAsync();

            // Assert
            Console.WriteLine("[Assert] Checking Manager Memory...");
            var result = await _manager.GetProvider("startup-provider");
            
            if (result == null) Console.WriteLine("[Error] Provider NOT found in memory!");
            else Console.WriteLine($"[Success] Provider found in memory: {result.ProviderId}");

            Assert.NotNull(result);
            Assert.Equal("startup-provider", result.ProviderId);
        }

        [Fact]
        public async Task RegisterProvider_ShouldCreateScope_AndSaveToDatabase()
        {
            Console.WriteLine("\n>>> TEST START: RegisterProvider");

            // Arrange
            var config = new Dictionary<string, string> { { "basePath", "C:/Data" } };
            var newProvider = new LocalProvider("dynamic-provider", "Local", config);

            // Act
            Console.WriteLine("[Act] Registering 'dynamic-provider'...");
            await _manager.RegisterProvider(newProvider);

            // Assert 1: In Memory
            var loaded = await _manager.GetProvider("dynamic-provider");
            Console.WriteLine($"[Assert] Memory Check: {(loaded != null ? "Found" : "Null")}");
            Assert.NotNull(loaded);

            // Assert 2: In Database
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NexusFSDbContext>();
                var entity = await db.Providers.FirstOrDefaultAsync(p => p.Id == "dynamic-provider");
                
                Console.WriteLine($"[Assert] DB Check: {(entity != null ? "Found" : "Null")}");
                if (entity != null) Console.WriteLine($"[Assert] DB Configuration: {entity.Configuration}");

                Assert.NotNull(entity);
                Assert.Equal("Local", entity.Type);
                Assert.Contains("C:/Data", entity.Configuration);
            }
        }

        [Fact]
        public async Task RemoveProvider_ShouldCreateScope_AndDeleteFromDatabase()
        {
            Console.WriteLine("\n>>> TEST START: RemoveProvider");

            // Arrange
            var validConfig = new Dictionary<string, string> { { "basePath", "C:/Temp/DeleteMe" } };
            var jsonConfig = JsonSerializer.Serialize(validConfig);

            Console.WriteLine($"[Arrange] Seeding 'to-delete' with Config: {jsonConfig}");
            
            var entity = new ProviderEntity 
            { 
                Id = "to-delete", 
                Name = "Delete Me", 
                Type = "Local", 
                IsActive = true,
                Configuration = jsonConfig
            };
            
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NexusFSDbContext>();
                db.Providers.Add(entity);
                await db.SaveChangesAsync();
            }
            
            // Load it so it exists in Manager's memory
            Console.WriteLine("[Arrange] Loading providers to memory...");
            await _manager.LoadProvidersFromDatabaseAsync();

            // Check if load was successful
            var checkMem = await _manager.GetProvider("to-delete");
            Console.WriteLine($"[Debug] Is 'to-delete' in memory before removal? {(checkMem != null ? "YES" : "NO")}");
            
            if (checkMem == null)
            {
                Console.WriteLine("[CRITICAL WARNING] Provider failed to load into memory. RemoveProvider will likely SKIP the DB delete because it checks memory first!");
            }

            // Act
            Console.WriteLine("[Act] Removing 'to-delete'...");
            await _manager.RemoveProvider("to-delete");

            // Assert 1: Memory gone
            var memResult = await _manager.GetProvider("to-delete");
            Console.WriteLine($"[Assert] Memory Check (Should be Null): {(memResult == null ? "Pass" : "Fail")}");
            Assert.Null(memResult);

            // Assert 2: DB gone
            using (var scope = _serviceProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IProviderRepository>();
                
                // Using GetByIdAsync (respects QueryFilters)
                var dbResult = await repo.GetByIdAsync("to-delete");
                Console.WriteLine($"[Assert] Repo GetByIdAsync (Should be Null): {(dbResult == null ? "Pass" : "Fail - Found Entity")}");
                Assert.Null(dbResult); 
                
                // Optional: Debug Raw DB
                var db = scope.ServiceProvider.GetRequiredService<NexusFSDbContext>();
                var raw = await db.Providers.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == "to-delete");
                
                if (raw != null)
                {
                     Console.WriteLine($"[Debug Raw DB] Entity ID: {raw.Id}");
                     Console.WriteLine($"[Debug Raw DB] IsActive: {raw.IsActive}");
                     Console.WriteLine($"[Debug Raw DB] DeletedAt: {raw.DeletedAt}");
                     
                     // Verify Soft Delete Logic
                     Assert.NotNull(raw.DeletedAt); 
                }
                else
                {
                    Console.WriteLine("[Debug Raw DB] Entity is completely gone (Hard Deleted).");
                }
            }
        }
    }
}