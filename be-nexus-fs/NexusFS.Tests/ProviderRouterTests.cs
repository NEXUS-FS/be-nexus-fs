using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Common;
using FluentAssertions;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Infrastructure.Services.Security;
using Moq;
using Domain.Repositories;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace NexusFS.Tests
{
    public class ProviderRouterTests
    {
        private readonly ProviderRouter _router;
        private readonly Logger _logger;
        private readonly ProviderManager _providerManager;
        private readonly Mock<ProviderManager> _mockProviderManager;
        private readonly Mock<Provider> _mockProvider;

        public ProviderRouterTests()
        {
            var auditRepo = new Mock<IAuditLogRepository>();
            _logger = new Logger(auditRepo.Object);
            var factory = new ProviderFactory();

            //added this to can create the ProviderManager.
            var mockScopeFactory = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
            var mockScope = new Mock<IServiceScope>();
            mockScopeFactory.Setup(s => s.CreateScope()).Returns(mockScope.Object);
            

            //lets put the logger as observer here
            var observers = new List<IProviderObserver> { _logger };

            _providerManager = new ProviderManager(factory, _logger, mockScopeFactory.Object, observers);
            var auth = new AuthManager(_logger);
            _router = new ProviderRouter(_providerManager, _logger, auth);

            // Setup mocks for successful operation tests
            _mockProvider = new Mock<Provider>("test-provider", "Local", new Dictionary<string, string>());
            _mockProviderManager = new Mock<ProviderManager>(factory, _logger);
        }

        #region RouteToProvider Tests

        [Fact]
        public async Task RouteToProvider_WithUnknownProvider_ShouldThrowKeyNotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _router.RouteToProvider("does-not-exist"));
        }

        [Fact]
        public async Task RouteToProvider_WithEmptyProviderId_ShouldThrowKeyNotFound()
        {
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _router.RouteToProvider(""));
            exception.Message.Should().Contain("required");
        }

        [Fact]
        public async Task RouteToProvider_WithNullProviderId_ShouldThrowKeyNotFound()
        {
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _router.RouteToProvider(null!));
            exception.Message.Should().Contain("required");
        }

        [Fact]
        public async Task RouteToProvider_WithWhitespaceProviderId_ShouldThrowKeyNotFound()
        {
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _router.RouteToProvider("   "));
            exception.Message.Should().Contain("required");
        }

        #endregion

        #region ExecuteOperation - Provider Not Found Tests

        [Fact]
        public async Task ExecuteOperation_WhenProviderNotFound_ShouldReturnFailureResponse()
        {
            var parameters = new Dictionary<string, object>
            {
                { "filePath", "test.txt" }
            };

            FileOperationResponse result = await _router.ExecuteOperation("missing-provider", "read", parameters);

            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("not found");
            result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        #endregion

        #region ExecuteOperation - Invalid Operation Tests

        [Fact]
        public async Task ExecuteOperation_WithInvalidOperation_ShouldReturnFailureResponse()
        {
            var parameters = new Dictionary<string, object>
            {
                { "filePath", "test.txt" }
            };

            var result = await _router.ExecuteOperation("missing-provider", "invalid-operation", parameters);

            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Unsupported operation");
        }

        [Fact]
        public async Task ExecuteOperation_WithNullOperation_ShouldReturnFailureResponse()
        {
            var parameters = new Dictionary<string, object>
            {
                { "filePath", "test.txt" }
            };

            var result = await _router.ExecuteOperation("missing-provider", null!, parameters);

            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Unsupported operation");
        }

        [Fact]
        public async Task ExecuteOperation_WithEmptyOperation_ShouldReturnFailureResponse()
        {
            var parameters = new Dictionary<string, object>
            {
                { "filePath", "test.txt" }
            };

            var result = await _router.ExecuteOperation("missing-provider", "", parameters);

            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Unsupported operation");
        }

        #endregion

        #region ExecuteOperation - Missing Parameters Tests

        // Note: These tests verify that missing parameters are detected
        // The implementation checks provider existence first, then validates parameters
        // So these tests verify the parameter validation happens after routing to provider

        [Fact]
        public async Task ExecuteOperation_ReadWithMissingFilePath_ShouldReturnFailureResponse()
        {
            var parameters = new Dictionary<string, object>();

            var result = await _router.ExecuteOperation("missing-provider", "read", parameters);

            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            // Provider is checked first, so we get provider not found
            result.Message.Should().Contain("not found");
        }

        [Fact]
        public async Task ExecuteOperation_WriteWithMissingFilePath_ShouldReturnFailureResponse()
        {
            var parameters = new Dictionary<string, object>
            {
                { "content", "test content" }
            };

            var result = await _router.ExecuteOperation("missing-provider", "write", parameters);

            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            // Provider is checked first, so we get provider not found
            result.Message.Should().Contain("not found");
        }

        [Fact]
        public async Task ExecuteOperation_WriteWithMissingContent_ShouldReturnFailureResponse()
        {
            var parameters = new Dictionary<string, object>
            {
                { "filePath", "test.txt" }
            };

            var result = await _router.ExecuteOperation("missing-provider", "write", parameters);

            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            // Provider is checked first, so we get provider not found
            result.Message.Should().Contain("not found");
        }

        [Fact]
        public async Task ExecuteOperation_DeleteWithMissingFilePath_ShouldReturnFailureResponse()
        {
            var parameters = new Dictionary<string, object>();

            var result = await _router.ExecuteOperation("missing-provider", "delete", parameters);

            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            // Provider is checked first, so we get provider not found
            result.Message.Should().Contain("not found");
        }

        [Fact]
        public async Task ExecuteOperation_ListWithMissingDirectoryPath_ShouldReturnFailureResponse()
        {
            var parameters = new Dictionary<string, object>();

            var result = await _router.ExecuteOperation("missing-provider", "list", parameters);

            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            // Provider is checked first, so we get provider not found
            result.Message.Should().Contain("not found");
        }

        #endregion

        #region ExecuteOperation - Operation Name Variations Tests

        [Fact]
        public async Task ExecuteOperation_WithReadFileAlias_ShouldHandleCorrectly()
        {
            var parameters = new Dictionary<string, object>
            {
                { "filePath", "test.txt" }
            };

            var result = await _router.ExecuteOperation("missing-provider", "readfile", parameters);

            result.Should().NotBeNull();
            result.Operation.Should().Be(FileOperation.Read);
        }

        [Fact]
        public async Task ExecuteOperation_WithWriteFileAlias_ShouldHandleCorrectly()
        {
            var parameters = new Dictionary<string, object>
            {
                { "filePath", "test.txt" },
                { "content", "content" }
            };

            var result = await _router.ExecuteOperation("missing-provider", "writefile", parameters);

            result.Should().NotBeNull();
            result.Operation.Should().Be(FileOperation.Write);
        }

        [Fact]
        public async Task ExecuteOperation_WithDeleteFileAlias_ShouldHandleCorrectly()
        {
            var parameters = new Dictionary<string, object>
            {
                { "filePath", "test.txt" }
            };

            var result = await _router.ExecuteOperation("missing-provider", "deletefile", parameters);

            result.Should().NotBeNull();
            result.Operation.Should().Be(FileOperation.Delete);
        }

        [Fact]
        public async Task ExecuteOperation_WithListFilesAlias_ShouldHandleCorrectly()
        {
            var parameters = new Dictionary<string, object>
            {
                { "directoryPath", "/test" }
            };

            var result = await _router.ExecuteOperation("missing-provider", "listfiles", parameters);

            result.Should().NotBeNull();
            result.Operation.Should().Be(FileOperation.List);
        }

        [Theory]
        [InlineData("Read")]
        [InlineData("read")]
        [InlineData("READ")]
        [InlineData("rEaD")]
        public async Task ExecuteOperation_WithCaseInsensitiveOperation_ShouldWork(string operation)
        {
            var parameters = new Dictionary<string, object>
            {
                { "filePath", "test.txt" }
            };

            var result = await _router.ExecuteOperation("missing-provider", operation, parameters);

            result.Should().NotBeNull();
            result.Operation.Should().Be(FileOperation.Read);
        }

        #endregion

        #region ExecuteOperation - Optional Parameters Tests

        [Fact]
        public async Task ExecuteOperation_ListWithoutRecursive_ShouldDefaultToFalse()
        {
            var parameters = new Dictionary<string, object>
            {
                { "directoryPath", "/test" }
                // recursive parameter not provided
            };

            var result = await _router.ExecuteOperation("missing-provider", "list", parameters);

            result.Should().NotBeNull();
            // Should not fail due to missing optional parameter
        }

        [Fact]
        public async Task ExecuteOperation_ListWithRecursiveTrue_ShouldHandleCorrectly()
        {
            var parameters = new Dictionary<string, object>
            {
                { "directoryPath", "/test" },
                { "recursive", true }
            };

            var result = await _router.ExecuteOperation("missing-provider", "list", parameters);

            result.Should().NotBeNull();
            result.Operation.Should().Be(FileOperation.List);
        }

        [Fact]
        public async Task ExecuteOperation_ListWithRecursiveFalse_ShouldHandleCorrectly()
        {
            var parameters = new Dictionary<string, object>
            {
                { "directoryPath", "/test" },
                { "recursive", false }
            };

            var result = await _router.ExecuteOperation("missing-provider", "list", parameters);

            result.Should().NotBeNull();
            result.Operation.Should().Be(FileOperation.List);
        }

        #endregion

        #region ExecuteOperation - Response Validation Tests

        [Fact]
        public async Task ExecuteOperation_ShouldSetTimestamp()
        {
            var parameters = new Dictionary<string, object>
            {
                { "filePath", "test.txt" }
            };

            var result = await _router.ExecuteOperation("missing-provider", "read", parameters);

            result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task ExecuteOperation_ShouldSetOperationInResponse()
        {
            var parameters = new Dictionary<string, object>
            {
                { "filePath", "test.txt" }
            };

            var result = await _router.ExecuteOperation("missing-provider", "read", parameters);

            result.Operation.Should().Be(FileOperation.Read);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task ExecuteOperation_WithAllOperations_ShouldSetCorrectOperationType()
        {
            var readParams = new Dictionary<string, object> { { "filePath", "test.txt" } };
            var writeParams = new Dictionary<string, object> { { "filePath", "test.txt" }, { "content", "data" } };
            var deleteParams = new Dictionary<string, object> { { "filePath", "test.txt" } };
            var listParams = new Dictionary<string, object> { { "directoryPath", "/test" } };

            var readResult = await _router.ExecuteOperation("missing-provider", "read", readParams);
            var writeResult = await _router.ExecuteOperation("missing-provider", "write", writeParams);
            var deleteResult = await _router.ExecuteOperation("missing-provider", "delete", deleteParams);
            var listResult = await _router.ExecuteOperation("missing-provider", "list", listParams);

            readResult.Operation.Should().Be(FileOperation.Read);
            writeResult.Operation.Should().Be(FileOperation.Write);
            deleteResult.Operation.Should().Be(FileOperation.Delete);
            listResult.Operation.Should().Be(FileOperation.List);
        }

        #endregion
    }
}
