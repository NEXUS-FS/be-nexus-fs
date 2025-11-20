using Application.DTOs.FileOperations;
using Application.UseCases.FileOperations.Commands;
using Application.UseCases.FileOperations.CommandsHandler;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace NexusFS.Tests.FileOperations
{
    public class WriteFileHandlerTests
    {
        private readonly Mock<IFileOperationRepository> _mockRepository;
        private readonly Mock<ILogger<WriteFileHandler>> _mockLogger;
        private readonly WriteFileHandler _handler;

        public WriteFileHandlerTests()
        {
            _mockRepository = new Mock<IFileOperationRepository>();
            _mockLogger = new Mock<ILogger<WriteFileHandler>>();
            _handler = new WriteFileHandler(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/file.txt",
                Content = "Hello World",
                UserId = "user-123"
            };
            var command = new WriteFileCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.WriteFileAsync(request.ProviderId, request.FilePath, request.Content))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("File written successfully");
            result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

            _mockRepository.Verify(r => r.ProviderExistsAsync(request.ProviderId), Times.Once);
            _mockRepository.Verify(r => r.WriteFileAsync(request.ProviderId, request.FilePath, request.Content), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithNonExistentProvider_ShouldReturnFailure()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = "non-existent-provider",
                FilePath = "/test/file.txt",
                Content = "Hello World",
                UserId = "user-123"
            };
            var command = new WriteFileCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("not found");

            _mockRepository.Verify(r => r.ProviderExistsAsync(request.ProviderId), Times.Once);
            _mockRepository.Verify(r => r.WriteFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithUnexpectedException_ShouldThrowException()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/file.txt",
                Content = "Hello World",
                UserId = "user-123"
            };
            var command = new WriteFileCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.WriteFileAsync(request.ProviderId, request.FilePath, request.Content))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.HandleAsync(command));
        }

        [Fact]
        public async Task HandleAsync_WithEmptyContent_ShouldStillSucceed()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/empty.txt",
                Content = "",
                UserId = "user-123"
            };
            var command = new WriteFileCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.WriteFileAsync(request.ProviderId, request.FilePath, request.Content))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            _mockRepository.Verify(r => r.WriteFileAsync(request.ProviderId, request.FilePath, ""), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldLogInformation()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/file.txt",
                Content = "content",
                UserId = "user-123"
            };
            var command = new WriteFileCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.WriteFileAsync(request.ProviderId, request.FilePath, request.Content))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Writing file")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
