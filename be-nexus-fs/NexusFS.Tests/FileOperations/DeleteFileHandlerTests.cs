using Application.DTOs.FileOperations;
using Application.UseCases.FileOperations.Commands;
using Application.UseCases.FileOperations.CommandsHandler;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace NexusFS.Tests.FileOperations
{
    public class DeleteFileHandlerTests
    {
        private readonly Mock<IFileOperationRepository> _mockRepository;
        private readonly Mock<ILogger<DeleteFileHandler>> _mockLogger;
        private readonly DeleteFileHandler _handler;

        public DeleteFileHandlerTests()
        {
            _mockRepository = new Mock<IFileOperationRepository>();
            _mockLogger = new Mock<ILogger<DeleteFileHandler>>();
            _handler = new DeleteFileHandler(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new DeleteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/file.txt",
                UserId = "user-123"
            };
            var command = new DeleteFileCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.DeleteFileAsync(request.ProviderId, request.FilePath))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("File deleted successfully");
            result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

            _mockRepository.Verify(r => r.ProviderExistsAsync(request.ProviderId), Times.Once);
            _mockRepository.Verify(r => r.DeleteFileAsync(request.ProviderId, request.FilePath), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithNonExistentProvider_ShouldReturnFailure()
        {
            // Arrange
            var request = new DeleteFileRequest
            {
                ProviderId = "non-existent-provider",
                FilePath = "/test/file.txt",
                UserId = "user-123"
            };
            var command = new DeleteFileCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("not found");

            _mockRepository.Verify(r => r.ProviderExistsAsync(request.ProviderId), Times.Once);
            _mockRepository.Verify(r => r.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithFileNotFoundException_ShouldReturnFailureWithMessage()
        {
            // Arrange
            var request = new DeleteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/nonexistent.txt",
                UserId = "user-123"
            };
            var command = new DeleteFileCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.DeleteFileAsync(request.ProviderId, request.FilePath))
                .ThrowsAsync(new FileNotFoundException("File not found"));

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("File not found");
        }

        [Fact]
        public async Task HandleAsync_WithUnexpectedException_ShouldThrowException()
        {
            // Arrange
            var request = new DeleteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/file.txt",
                UserId = "user-123"
            };
            var command = new DeleteFileCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.DeleteFileAsync(request.ProviderId, request.FilePath))
                .ThrowsAsync(new InvalidOperationException("Unexpected error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(command));
        }

        [Fact]
        public async Task HandleAsync_ShouldLogInformation()
        {
            // Arrange
            var request = new DeleteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/file.txt",
                UserId = "user-123"
            };
            var command = new DeleteFileCommand { Request = request };

            _mockRepository.Setup(r => r.ProviderExistsAsync(request.ProviderId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.DeleteFileAsync(request.ProviderId, request.FilePath))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.HandleAsync(command);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleting file")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
