using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using be_nexus_fs.Controllers;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NexusFS.Tests.Controllers;

public class CredentialRotationControllerTests
{
    private static (CredentialRotationController controller, Mock<IProviderRepository> repo) BuildController(string providerType = "memory", string? configJson = "{}")
    {
        var repo = new Mock<IProviderRepository>();
        var scope = new Mock<IServiceScope>();
        var sp = new Mock<IServiceProvider>();
        sp.Setup(p => p.GetService(typeof(IProviderRepository))).Returns(repo.Object);
        scope.Setup(s => s.ServiceProvider).Returns(sp.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var auditRepo = new Mock<IAuditLogRepository>();
        var logger = new Logger(auditRepo.Object);
        var providerManager = new ProviderManager(new ProviderFactory(), logger, scopeFactory.Object, Array.Empty<IProviderObserver>());

        var controllerLogger = new Mock<ILogger<CredentialRotationController>>();
        var controller = new CredentialRotationController(repo.Object, providerManager, controllerLogger.Object);

        // Default repo return
        repo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new ProviderEntity
        {
            Id = "prov-1",
            Name = "prov-1",
            Type = providerType,
            Configuration = configJson,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        repo.Setup(r => r.UpdateAsync(It.IsAny<ProviderEntity>())).Returns(Task.CompletedTask);

        return (controller, repo);
    }

    [Fact]
    public async Task RotateCredentials_ReturnsOk_WhenValid()
    {
        var (controller, repo) = BuildController();
        var request = new RotateCredentialsRequest
        {
            NewCredentials = new Dictionary<string, string> { { "token", "abc" } }
        };

        var result = await controller.RotateCredentials("prov-1", request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<RotateCredentialsResponse>(ok.Value);
        Assert.True(payload.Success);
        repo.Verify(r => r.UpdateAsync(It.Is<ProviderEntity>(p => p.Configuration!.Contains("token"))), Times.Once);
    }

    [Fact]
    public async Task RotateCredentials_ReturnsNotFound_WhenProviderMissing()
    {
        var (controller, repo) = BuildController();
        repo.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((ProviderEntity?)null);

        var result = await controller.RotateCredentials("missing", new RotateCredentialsRequest { NewCredentials = new() });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task RotateCredentials_ReturnsBadRequest_WhenProviderTypeUnsupported()
    {
        var (controller, repo) = BuildController(providerType: "unsupported");

        var result = await controller.RotateCredentials("prov-1", new RotateCredentialsRequest { NewCredentials = new() });

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var payload = Assert.IsType<RotateCredentialsResponse>(bad.Value);
        Assert.False(payload.Success);
    }

    [Fact]
    public async Task TestCredentials_ReturnsOk()
    {
        var (controller, repo) = BuildController();
        var result = await controller.TestCredentials("prov-1", new TestCredentialsRequest { Credentials = new() });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<TestCredentialsResponse>(ok.Value);
        Assert.True(payload.Success);
    }

    [Fact]
    public async Task TestCredentials_NotFound()
    {
        var (controller, repo) = BuildController();
        repo.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((ProviderEntity?)null);

        var result = await controller.TestCredentials("missing", new TestCredentialsRequest { Credentials = new() });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetRotationHistory_ReturnsOk()
    {
        var (controller, _) = BuildController(configJson: "{\"token\":\"abc\"}");

        var result = await controller.GetRotationHistory("prov-1");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<CredentialHistoryResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("prov-1", payload.ProviderId);
    }
}
