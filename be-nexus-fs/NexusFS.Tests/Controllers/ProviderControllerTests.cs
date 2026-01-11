using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using be_nexus_fs.Controllers;
using Domain.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NexusFS.Tests.Controllers;

public class ProviderControllerTests
{
    private static ProviderManager BuildProviderManager(Mock<IProviderRepository> repoMock)
    {
        var auditRepo = new Mock<IAuditLogRepository>();
        var logger = new Logger(auditRepo.Object);
        var scope = new Mock<IServiceScope>();
        var sp = new Mock<IServiceProvider>();
        sp.Setup(p => p.GetService(typeof(IProviderRepository))).Returns(repoMock.Object);
        scope.Setup(s => s.ServiceProvider).Returns(sp.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        return new ProviderManager(
            new ProviderFactory(),
            logger,
            scopeFactory.Object,
            Array.Empty<IProviderObserver>());
    }

    [Fact]
    public async Task RegisterGoogleDriveProvider_ReturnsOk_WhenValid()
    {
        // Arrange
        var repoMock = new Mock<IProviderRepository>();
        repoMock.Setup(r => r.GetByIdAsync("mem-1")).ReturnsAsync((Domain.Entities.ProviderEntity?)null);
        repoMock.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.ProviderEntity>()))
            .ReturnsAsync(new Domain.Entities.ProviderEntity
            {
                Id = "mem-1",
                Name = "mem-1",
                Type = "Memory",
                IsActive = true
            });

        var manager = BuildProviderManager(repoMock);
        var controllerLogger = new Mock<ILogger<ProviderController>>();
        var controller = new ProviderController(new ProviderFactory(), manager, controllerLogger.Object);

        var request = new ProviderRegistrationRequest
        {
            ProviderId = "mem-1",
            ProviderType = "memory",
            Configuration = new Dictionary<string, string> { { "basePath", "/tmp" } }
        };

        // Act
        var result = await controller.RegisterGoogleDriveProvider(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = ok.Value!;
        var providerId = payload.GetType().GetProperty("providerId")?.GetValue(payload, null);
        var providerType = payload.GetType().GetProperty("providerType")?.GetValue(payload, null);
        Assert.Equal("mem-1", providerId);
        Assert.Equal("Memory", providerType);
        repoMock.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.ProviderEntity>()), Times.Once);
    }

    [Fact]
    public async Task RegisterGoogleDriveProvider_ReturnsBadRequest_WhenUnsupported()
    {
        var manager = BuildProviderManager(new Mock<IProviderRepository>());
        var controllerLogger = new Mock<ILogger<ProviderController>>();
        var controller = new ProviderController(new ProviderFactory(), manager, controllerLogger.Object);

        var request = new ProviderRegistrationRequest
        {
            ProviderId = "x",
            ProviderType = "unsupported",
            Configuration = new Dictionary<string, string>()
        };

        var result = await controller.RegisterGoogleDriveProvider(request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("not supported", bad.Value!.ToString());
    }

    [Fact]
    public async Task RegisterGoogleDriveProvider_ReturnsBadRequest_WhenProviderIdMissing()
    {
        var manager = BuildProviderManager(new Mock<IProviderRepository>());
        var controllerLogger = new Mock<ILogger<ProviderController>>();
        var controller = new ProviderController(new ProviderFactory(), manager, controllerLogger.Object);

        var request = new ProviderRegistrationRequest
        {
            ProviderId = "",
            ProviderType = "memory",
            Configuration = new Dictionary<string, string>()
        };

        var result = await controller.RegisterGoogleDriveProvider(request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("ProviderId is required", bad.Value!.ToString());
    }
}
