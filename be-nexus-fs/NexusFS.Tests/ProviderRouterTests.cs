using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Repositories;
using FluentAssertions;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Infrastructure.Services.Security;
using Moq;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace NexusFS.Tests
{
    public class ProviderRouterTests
    {
        private ProviderManager CreateManagerWithMemoryProvider(string providerId = "mem-rt")
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

            var mem = new MemoryProvider(providerId);
            mgr.RegisterProvider(mem).GetAwaiter().GetResult();
            return mgr;
        }

        [Fact]
        public async Task RouteToProvider_ReturnsProvider_WhenExists()
        {
            var mgr = CreateManagerWithMemoryProvider("mem-good");
            var auditRepo = new Mock<IAuditLogRepository>();
            var logger = new Logger(auditRepo.Object);
            var authManager = new AuthManager(logger);

            var router = new ProviderRouter(mgr, logger, authManager);

            var provider = await router.RouteToProvider("mem-good");
            provider.Should().NotBeNull();
            provider.ProviderId.Should().Be("mem-good");
        }

        [Fact]
        public async Task ExecuteOperation_ReadAndWrite_WorkFlow()
        {
            var mgr = CreateManagerWithMemoryProvider("mem-flow");
            var auditRepo = new Mock<IAuditLogRepository>();
            var logger = new Logger(auditRepo.Object);
            var authManager = new AuthManager(logger);

            var router = new ProviderRouter(mgr, logger, authManager);

            var writeResp = await router.ExecuteOperation("mem-flow", "write", new Dictionary<string, object>
            {
                { "filePath", "folder/a.txt" },
                { "content", "hello" }
            });

            writeResp.Success.Should().BeTrue();

            var readResp = await router.ExecuteOperation("mem-flow", "read", new Dictionary<string, object>
            {
                { "filePath", "folder/a.txt" }
            });

            readResp.Success.Should().BeTrue();
            readResp.Content.Should().Be("hello");
        }
    }
}
