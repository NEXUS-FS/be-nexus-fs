using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace NexusFS.Tests
{
    public class ProviderManagerTests
    {
        private (ProviderManager mgr, Mock<IProviderRepository> repoMock) CreateManager()
        {
            var auditRepo = new Mock<IAuditLogRepository>();
            var logger = new Logger(auditRepo.Object);
            var factory = new ProviderFactory();

            var repo = new Mock<IProviderRepository>();

            var serviceProvider = new Mock<System.IServiceProvider>();
            serviceProvider.Setup(p => p.GetService(typeof(IProviderRepository))).Returns(repo.Object);

            var scope = new Mock<IServiceScope>();
            scope.SetupGet(s => s.ServiceProvider).Returns(serviceProvider.Object);

            var scopeFactory = new Mock<IServiceScopeFactory>();
            scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

            var observers = new List<IProviderObserver> { logger };
            var mgr = new ProviderManager(factory, logger, scopeFactory.Object, observers);
            return (mgr, repo);
        }

        [Fact]
        public async Task LoadProvidersFromDatabaseAsync_LoadsSupportedProviders_SkipsUnsupported()
        {
            var (mgr, repo) = CreateManager();

            repo.Setup(r => r.GetActiveProvidersAsync()).ReturnsAsync(new List<ProviderEntity>
            {
                new ProviderEntity { Id = "p1", Name = "Local One", Type = "Local", IsActive = true, Configuration = "{\"basePath\":\"/tmp\"}" },
                new ProviderEntity { Id = "bad", Name = "Bad", Type = "UnknownType", IsActive = true, Configuration = "{}" }
            });

            await mgr.LoadProvidersFromDatabaseAsync();

            var all = await mgr.GetAllProviders();
            all.Should().HaveCount(1);
            (await mgr.GetProvider("p1")).Should().NotBeNull();
            (await mgr.GetProvider("bad")).Should().BeNull();
        }

        [Fact]
        public async Task RegisterProvider_AddsAndPersists()
        {
            var (mgr, repo) = CreateManager();
            var provider = new MemoryProvider("mem-1");
            await mgr.RegisterProvider(provider);

            (await mgr.GetProvider("mem-1")).Should().NotBeNull();
            repo.Verify(r => r.GetByIdAsync("mem-1"), Times.Once);
        }

        [Fact]
        public async Task RemoveProvider_RemovesAndDeletes()
        {
            var (mgr, repo) = CreateManager();
            var provider = new MemoryProvider("mem-1");
            await mgr.RegisterProvider(provider);

            await mgr.RemoveProvider("mem-1");
            (await mgr.GetProvider("mem-1")).Should().BeNull();
            repo.Verify(r => r.DeleteAsync("mem-1"), Times.Once);
        }

        [Fact]
        public async Task ReloadProvider_ReplacesInMemoryFromDatabase()
        {
            var (mgr, repo) = CreateManager();
            var provider = new MemoryProvider("mem-1");
            await mgr.RegisterProvider(provider);

            repo.Setup(r => r.GetByIdAsync("mem-1")).ReturnsAsync(new ProviderEntity
            {
                Id = "mem-1",
                Name = "Memory",
                Type = "Memory",
                IsActive = true,
                Configuration = "{}"
            });

            await mgr.ReloadProvider("mem-1");
            var reloaded = await mgr.GetProvider("mem-1");
            reloaded.Should().NotBeNull();
            reloaded!.ProviderType.Should().Be("Memory");
        }

        [Fact]
        public async Task CreateAndTestProvider_SupportedType_ReturnsSuccess()
        {
            var (mgr, _) = CreateManager();
            var result = await mgr.CreateAndTestProvider("memory", "check-1", new Dictionary<string, string>());
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("valid");
        }

        [Fact]
        public async Task CreateAndTestProvider_UnsupportedType_ReturnsFailure()
        {
            var (mgr, _) = CreateManager();
            var result = await mgr.CreateAndTestProvider("unknown", "x", new Dictionary<string, string>());
            result.Success.Should().BeFalse();
        }
    }
}
