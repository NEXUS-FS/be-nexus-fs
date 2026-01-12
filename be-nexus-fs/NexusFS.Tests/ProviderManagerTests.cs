using FluentAssertions;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Domain.Repositories;

namespace NexusFS.Tests;

public class ProviderManagerTests
{
    private readonly ProviderFactory _providerFactory;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Logger _logger;
    private readonly MetricsCollector _metricsCollector;

    public ProviderManagerTests()
    {
        _providerFactory = new ProviderFactory();

        // Setup repository mocks
        var mockAuditLogRepository = new Mock<IAuditLogRepository>();
        var mockMetricRepository = new Mock<IMetricRepository>();
        var mockProviderRepository = new Mock<IProviderRepository>();

        // Setup real service collection and provider to support GetRequiredService extension method
        var services = new ServiceCollection();
        services.AddSingleton(mockProviderRepository.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Create a scope factory that creates new scopes each time
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory
            .Setup(s => s.CreateScope())
            .Returns(() => serviceProvider.CreateScope());
        _serviceScopeFactory = mockScopeFactory.Object;

        // Create real observer instances for integration-style tests
        _logger = new Logger(mockAuditLogRepository.Object);
        _metricsCollector = new MetricsCollector(mockMetricRepository.Object, _logger);
    }

    private ProviderManager CreateProviderManager(IEnumerable<IProviderObserver>? observers = null)
    {
        observers ??= new List<IProviderObserver> { _logger, _metricsCollector };
        return new ProviderManager(_providerFactory, _logger, _serviceScopeFactory, observers);
    }

    private Provider CreateTestProvider(string providerId = "test-provider")
    {
        var provider = new LocalProvider(providerId);
        var config = new Dictionary<string, string> { { "basePath", Path.GetTempPath() } };
        provider.Initialize(config).GetAwaiter().GetResult();
        return provider;
    }


    [Fact]
    public async Task RegisterProvider_ShouldAddProviderToRegistry()
    {
        var manager = CreateProviderManager();
        var provider = CreateTestProvider("provider-1");

        await manager.RegisterProvider(provider);

        var retrieved = await manager.GetProvider("provider-1");
        retrieved.Should().NotBeNull();
        retrieved.ProviderId.Should().Be("provider-1");
    }

    [Fact]
    public async Task RegisterProvider_ShouldNotifyObservers()
    {
        var mockObserver = new Mock<IProviderObserver>();
        var observers = new List<IProviderObserver> { mockObserver.Object };
        var manager = CreateProviderManager(observers);
        var provider = CreateTestProvider("provider-1");

        await manager.RegisterProvider(provider);

        mockObserver.Verify(
            o => o.OnProviderRegistered("provider-1", "Local"),
            Times.Once,
            "Observer should be notified when provider is registered");
    }

    [Fact]
    public async Task RegisterProvider_ShouldNotifyMultipleObservers()
    {
        var mockObserver1 = new Mock<IProviderObserver>();
        var mockObserver2 = new Mock<IProviderObserver>();
        var observers = new List<IProviderObserver> { mockObserver1.Object, mockObserver2.Object };
        var manager = CreateProviderManager(observers);
        var provider = CreateTestProvider("provider-1");

        await manager.RegisterProvider(provider);

        mockObserver1.Verify(
            o => o.OnProviderRegistered("provider-1", "Local"),
            Times.Once);
        mockObserver2.Verify(
            o => o.OnProviderRegistered("provider-1", "Local"),
            Times.Once);
    }

    [Fact]
    public async Task RegisterProvider_ShouldNotAddDuplicateProvider()
    {
        var manager = CreateProviderManager();
        var provider = CreateTestProvider("provider-1");

        await manager.RegisterProvider(provider);
        await manager.RegisterProvider(provider); // Try to register again

        var allProviders = await manager.GetAllProviders();
        allProviders.Should().HaveCount(1);
    }

    [Fact]
    public async Task RegisterProvider_WithNullProvider_ShouldThrowArgumentNullException()
    {
        var manager = CreateProviderManager();

        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.RegisterProvider(null!));
    }


    [Fact]
    public async Task GetProvider_ShouldRetrieveProviderById()
    {
        var manager = CreateProviderManager();
        var provider = CreateTestProvider("provider-1");
        await manager.RegisterProvider(provider);

        var retrieved = await manager.GetProvider("provider-1");

        retrieved.Should().NotBeNull();
        retrieved.ProviderId.Should().Be("provider-1");
        retrieved.ProviderType.Should().Be("Local");
    }

    [Fact]
    public async Task GetProvider_WithNonExistentId_ShouldReturnNull()
    {
        var manager = CreateProviderManager();

        var retrieved = await manager.GetProvider("non-existent");

        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task GetProvider_ShouldRetrieveCorrectProvider()
    {
        var manager = CreateProviderManager();
        var provider1 = CreateTestProvider("provider-1");
        var provider2 = CreateTestProvider("provider-2");
        await manager.RegisterProvider(provider1);
        await manager.RegisterProvider(provider2);

        var retrieved1 = await manager.GetProvider("provider-1");
        var retrieved2 = await manager.GetProvider("provider-2");

        retrieved1!.ProviderId.Should().Be("provider-1");
        retrieved2!.ProviderId.Should().Be("provider-2");
    }


    [Fact]
    public async Task RemoveProvider_ShouldRemoveProviderFromRegistry()
    {
        var manager = CreateProviderManager();
        var provider = CreateTestProvider("provider-1");
        await manager.RegisterProvider(provider);

        await manager.RemoveProvider("provider-1");

        var retrieved = await manager.GetProvider("provider-1");
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task RemoveProvider_ShouldNotifyObservers()
    {
        // Arrange
        var mockObserver = new Mock<IProviderObserver>();
        var observers = new List<IProviderObserver> { mockObserver.Object };
        var manager = CreateProviderManager(observers);
        var provider = CreateTestProvider("provider-1");
        await manager.RegisterProvider(provider);

        // Act
        await manager.RemoveProvider("provider-1");

        // Assert
        mockObserver.Verify(
            o => o.OnProviderRemoved("provider-1"),
            Times.Once,
            "Observer should be notified when provider is removed");
    }

    [Fact]
    public async Task RemoveProvider_ShouldNotifyMultipleObservers()
    {
        var mockObserver1 = new Mock<IProviderObserver>();
        var mockObserver2 = new Mock<IProviderObserver>();
        var observers = new List<IProviderObserver> { mockObserver1.Object, mockObserver2.Object };
        var manager = CreateProviderManager(observers);
        var provider = CreateTestProvider("provider-1");
        await manager.RegisterProvider(provider);

        await manager.RemoveProvider("provider-1");

        mockObserver1.Verify(
            o => o.OnProviderRemoved("provider-1"),
            Times.Once);
        mockObserver2.Verify(
            o => o.OnProviderRemoved("provider-1"),
            Times.Once);
    }

    [Fact]
    public async Task RemoveProvider_WithNonExistentId_ShouldNotNotifyObservers()
    {
        var mockObserver = new Mock<IProviderObserver>();
        var observers = new List<IProviderObserver> { mockObserver.Object };
        var manager = CreateProviderManager(observers);

        await manager.RemoveProvider("non-existent");

        mockObserver.Verify(
            o => o.OnProviderRemoved(It.IsAny<string>()),
            Times.Never,
            "Observer should not be notified when removing non-existent provider");
    }


    [Fact]
    public async Task RegisterObserver_ShouldAddObserverToList()
    {
        var manager = CreateProviderManager(new List<IProviderObserver>());
        var mockObserver = new Mock<IProviderObserver>();

        manager.RegisterObserver(mockObserver.Object);
        var provider = CreateTestProvider("provider-1");
        await manager.RegisterProvider(provider);

        mockObserver.Verify(
            o => o.OnProviderRegistered("provider-1", "Local"),
            Times.Once,
            "Newly registered observer should receive notifications");
    }

    [Fact]
    public void RegisterObserver_WithNullObserver_ShouldThrowArgumentNullException()
    {
        // Arrange
        var manager = CreateProviderManager(new List<IProviderObserver>());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.RegisterObserver(null!));
    }

    [Fact]
    public async Task RegisterObserver_ShouldAllowMultipleObservers()
    {
        var manager = CreateProviderManager(new List<IProviderObserver>());
        var mockObserver1 = new Mock<IProviderObserver>();
        var mockObserver2 = new Mock<IProviderObserver>();

        manager.RegisterObserver(mockObserver1.Object);
        manager.RegisterObserver(mockObserver2.Object);
        var provider = CreateTestProvider("provider-1");
        await manager.RegisterProvider(provider);

        mockObserver1.Verify(
            o => o.OnProviderRegistered("provider-1", "Local"),
            Times.Once);
        mockObserver2.Verify(
            o => o.OnProviderRegistered("provider-1", "Local"),
            Times.Once);
    }


    [Fact]
    public async Task RegisterProvider_ShouldNotifyLoggerObserver()
    {
        var manager = CreateProviderManager(new List<IProviderObserver> { _logger });
        var provider = CreateTestProvider("provider-1");

        await manager.RegisterProvider(provider);

        var retrieved = await manager.GetProvider("provider-1");
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterProvider_ShouldNotifyMetricsCollectorObserver()
    {
        var manager = CreateProviderManager(new List<IProviderObserver> { _metricsCollector });
        var provider = CreateTestProvider("provider-1");

        await manager.RegisterProvider(provider);

        var metrics = await _metricsCollector.GetMetricsAsync(providerId: "provider-1");
        metrics.Should().Contain(m => m.MetricName == "provider.registered");
    }

    [Fact]
    public async Task RemoveProvider_ShouldNotifyMetricsCollectorObserver()
    {
        var manager = CreateProviderManager(new List<IProviderObserver> { _metricsCollector });
        var provider = CreateTestProvider("provider-1");
        await manager.RegisterProvider(provider);

        await manager.RemoveProvider("provider-1");

        var metrics = await _metricsCollector.GetMetricsAsync(providerId: "provider-1");
        metrics.Should().Contain(m => m.MetricName == "provider.removed");
    }

    [Fact]
    public async Task ObserverFailure_ShouldNotCrashProviderManager()
    {
        var failingObserver = new Mock<IProviderObserver>();
        failingObserver
            .Setup(o => o.OnProviderRegistered(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Observer failure"));

        var workingObserver = new Mock<IProviderObserver>();
        var observers = new List<IProviderObserver> { failingObserver.Object, workingObserver.Object };
        var manager = CreateProviderManager(observers);
        var provider = CreateTestProvider("provider-1");

        await manager.RegisterProvider(provider);

        workingObserver.Verify(
            o => o.OnProviderRegistered("provider-1", "Local"),
            Times.Once,
            "Working observer should still receive notification even if another fails");

        var retrieved = await manager.GetProvider("provider-1");
        retrieved.Should().NotBeNull();
    }


    [Fact]
    public async Task GetAllProviders_ShouldReturnAllRegisteredProviders()
    {
        var manager = CreateProviderManager();
        var provider1 = CreateTestProvider("provider-1");
        var provider2 = CreateTestProvider("provider-2");
        await manager.RegisterProvider(provider1);
        await manager.RegisterProvider(provider2);

        var allProviders = await manager.GetAllProviders();

        var enumerable = allProviders as Provider[] ?? allProviders.ToArray();
        enumerable.Should().HaveCount(2);
        enumerable.Should().Contain(p => p.ProviderId == "provider-1");
        enumerable.Should().Contain(p => p.ProviderId == "provider-2");
    }

    [Fact]
    public async Task GetAllProviders_WithNoProviders_ShouldReturnEmptyList()
    {
        var manager = CreateProviderManager();

        var allProviders = await manager.GetAllProviders();

        allProviders.Should().BeEmpty();
    }
}
