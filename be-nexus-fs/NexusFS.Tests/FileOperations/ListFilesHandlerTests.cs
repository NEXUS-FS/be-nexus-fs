using Application.DTOs.FileOperations;
using Application.UseCases.FileOperations.Commands;
using Application.UseCases.FileOperations.CommandsHandler;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace NexusFS.Tests.FileOperations
{
    public class ListFilesHandlerTests
    {
        private readonly Mock<IFileOperationRepository> _mockRepository;
        private readonly Mock<ILogger<ListFilesHandler>> _mockLogger;
        private readonly ListFilesHandler _handler;

        public ListFilesHandlerTests()
        {
            _mockRepository = new Mock<IFileOperationRepository>();
            _mockLogger = new Mock<ILogger<ListFilesHandler>>();
            _handler = new ListFilesHandler(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_WithValidRequest_ShouldReturnSuccessWithFiles()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = "local-provider",
                DirectoryPath = "/test",
                Recursive = false,
                UserId = "user-123"
            };
            var command = new ListFilesCommand { Request = request };
            var expectedFiles = new List<string> { "/test/file1.txt", "/test/file2.txt", "/test/file3.txt" };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.ListFilesAsync(request.ProviderId, request.DirectoryPath, request.Recursive))
                .ReturnsAsync(expectedFiles);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Files listed successfully");
            result.Files.Should().BeEquivalentTo(expectedFiles);
            result.DirectoryPath.Should().Be(request.DirectoryPath);
            result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

            _mockRepository.Verify(r => r.ProviderExistsAsync(request.ProviderId), Times.Once);
            _mockRepository.Verify(r => r.ListFilesAsync(request.ProviderId, request.DirectoryPath, request.Recursive), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithRecursiveTrue_ShouldPassRecursiveFlag()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = "local-provider",
                DirectoryPath = "/test",
                Recursive = true,
                UserId = "user-123"
            };
            var command = new ListFilesCommand { Request = request };
            var expectedFiles = new List<string> { "/test/file1.txt", "/test/subdir/file2.txt" };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.ListFilesAsync(request.ProviderId, request.DirectoryPath, true))
                .ReturnsAsync(expectedFiles);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Files.Should().HaveCount(2);
            _mockRepository.Verify(r => r.ListFilesAsync(request.ProviderId, request.DirectoryPath, true), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithEmptyDirectory_ShouldReturnSuccessWithEmptyList()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = "local-provider",
                DirectoryPath = "/empty",
                Recursive = false,
                UserId = "user-123"
            };
            var command = new ListFilesCommand { Request = request };
            var expectedFiles = new List<string>();

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.ListFilesAsync(request.ProviderId, request.DirectoryPath, request.Recursive))
                .ReturnsAsync(expectedFiles);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Files.Should().BeEmpty();
        }

        [Fact]
        public async Task HandleAsync_WithNonExistentProvider_ShouldReturnFailure()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = "non-existent-provider",
                DirectoryPath = "/test",
                Recursive = false,
                UserId = "user-123"
            };
            var command = new ListFilesCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("not found");
            result.Files.Should().BeNull();

            _mockRepository.Verify(r => r.ProviderExistsAsync(request.ProviderId), Times.Once);
            _mockRepository.Verify(r => r.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithDirectoryNotFoundException_ShouldReturnFailureWithMessage()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = "local-provider",
                DirectoryPath = "/nonexistent",
                Recursive = false,
                UserId = "user-123"
            };
            var command = new ListFilesCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.ListFilesAsync(request.ProviderId, request.DirectoryPath, request.Recursive))
                .ThrowsAsync(new DirectoryNotFoundException("Directory not found"));

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Directory not found");
            result.Files.Should().BeNull();
        }

        [Fact]
        public async Task HandleAsync_WithUnexpectedException_ShouldThrowException()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = "local-provider",
                DirectoryPath = "/test",
                Recursive = false,
                UserId = "user-123"
            };
            var command = new ListFilesCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.ListFilesAsync(request.ProviderId, request.DirectoryPath, request.Recursive))
                .ThrowsAsync(new InvalidOperationException("Unexpected error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(command));
        }

        [Fact]
        public async Task HandleAsync_ShouldLogInformation()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = "local-provider",
                DirectoryPath = "/test",
                Recursive = false,
                UserId = "user-123"
            };
            var command = new ListFilesCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.ListFilesAsync(request.ProviderId, request.DirectoryPath, request.Recursive))
                .ReturnsAsync(new List<string>());

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Listing files")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
