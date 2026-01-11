using Infrastructure.Services;
using Infrastructure.Services.Decorators;
using Infrastructure.Services.Observability;
using Domain.Repositories;
using Moq;
using FluentAssertions;
using Xunit;

namespace NexusFS.Tests
{
    public class CircuitBreakerProviderDecoratorTests
    {
        private readonly LocalProvider _provider;
        private readonly Logger _logger;

        public CircuitBreakerProviderDecoratorTests()
        {
            _provider = new LocalProvider("test-provider");
            var config = new Dictionary<string, string> { { "basePath", Path.GetTempPath() } };
            _provider.Initialize(config).Wait();
            var auditRepo = new Mock<IAuditLogRepository>();
            _logger = new Logger(auditRepo.Object);
        }

        [Fact]
        public async Task ReadFileAsync_WithSuccessfulOperation_ShouldReturnContent()
        {
            // Arrange
            var testFile = Path.Combine(Path.GetTempPath(), "test-circuit-breaker.txt");
            var expectedContent = "file content";
            await File.WriteAllTextAsync(testFile, expectedContent);

            var decorator = new CircuitBreakerProviderDecorator(
                _provider,
                _logger,
                failureThreshold: 3,
                breakDuration: TimeSpan.FromSeconds(30));

            try
            {
                // Act
                var result = await decorator.ReadFileAsync("test-circuit-breaker.txt");

                // Assert
                result.Should().Be(expectedContent);
            }
            finally
            {
                // Cleanup
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        [Fact]
        public async Task ReadFileAsync_WithFailedOperation_ShouldThrow()
        {
            // Arrange
            var decorator = new CircuitBreakerProviderDecorator(
                _provider,
                _logger,
                failureThreshold: 3,
                breakDuration: TimeSpan.FromSeconds(30));

            // Act & Assert - trying to read non-existent file
            await Assert.ThrowsAsync<FileNotFoundException>(() => decorator.ReadFileAsync("nonexistent-file.txt"));
        }

        [Fact]
        public async Task WriteFileAsync_WithSuccessfulOperation_ShouldComplete()
        {
            // Arrange
            var testFile = Path.Combine(Path.GetTempPath(), "test-write-circuit.txt");
            var decorator = new CircuitBreakerProviderDecorator(
                _provider,
                _logger);

            try
            {
                // Act
                await decorator.WriteFileAsync("test-write-circuit.txt", "content");

                // Assert
                File.Exists(testFile).Should().BeTrue();
                var content = await File.ReadAllTextAsync(testFile);
                content.Should().Be("content");
            }
            finally
            {
                // Cleanup
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        [Fact]
        public async Task TestConnectionAsync_WithSuccessfulConnection_ShouldReturnTrue()
        {
            // Arrange
            var decorator = new CircuitBreakerProviderDecorator(
                _provider,
                _logger);

            // Act
            var result = await decorator.TestConnectionAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void DecoratedProvider_ShouldReturnOriginalProvider()
        {
            // Arrange
            var decorator = new CircuitBreakerProviderDecorator(
                _provider,
                _logger);

            // Act
            var result = decorator.DecoratedProvider;

            // Assert
            result.Should().Be(_provider);
        }
    }
}

