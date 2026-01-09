using Application.UseCases.FileOperations.CommandsHandler;
using Application.UseCases.FileOperations.Commands;
using Application.DTOs.FileOperations;
using Domain.Repositories;
using Moq;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Logging;

namespace NexusFS.Tests.FileOperations
{
    public class ExistsHandlerTests
    {
        private readonly Mock<IFileOperationRepository> _repositoryMock;
        private readonly Mock<ILogger<ExistsHandler>> _loggerMock;
        private readonly ExistsHandler _handler;

        public ExistsHandlerTests()
        {
            _repositoryMock = new Mock<IFileOperationRepository>();
            _loggerMock = new Mock<ILogger<ExistsHandler>>();
            _handler = new ExistsHandler(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WithExistingFile_ShouldReturnTrue()
        {
            // Arrange
            var command = new ExistsCommand
            {
                Request = new ExistsRequest
                {
                    ProviderId = "test-provider",
                    Path = "/test/existing-file.txt",
                    UserId = "test-user"
                }
            };

            _repositoryMock.Setup(x => x.ProviderExistsAsync(command.Request.ProviderId))
                .ReturnsAsync(true);
            _repositoryMock.Setup(x => x.ExistsAsync(command.Request.ProviderId, command.Request.Path))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Exists.Should().BeTrue();
            result.Path.Should().Be("/test/existing-file.txt");
        }

        [Fact]
        public async Task HandleAsync_WithNonExistentFile_ShouldReturnFalse()
        {
            // Arrange
            var command = new ExistsCommand
            {
                Request = new ExistsRequest
                {
                    ProviderId = "test-provider",
                    Path = "/test/nonexistent.txt",
                    UserId = "test-user"
                }
            };

            _repositoryMock.Setup(x => x.ProviderExistsAsync(command.Request.ProviderId))
                .ReturnsAsync(true);
            _repositoryMock.Setup(x => x.ExistsAsync(command.Request.ProviderId, command.Request.Path))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Exists.Should().BeFalse();
        }

        [Fact]
        public async Task HandleAsync_WithRepositoryError_ShouldThrow()
        {
            // Arrange
            var command = new ExistsCommand
            {
                Request = new ExistsRequest
                {
                    ProviderId = "test-provider",
                    Path = "/test/file.txt",
                    UserId = "test-user"
                }
            };

            _repositoryMock.Setup(x => x.ProviderExistsAsync(command.Request.ProviderId))
                .ReturnsAsync(true);
            _repositoryMock.Setup(x => x.ExistsAsync(command.Request.ProviderId, command.Request.Path))
                .ThrowsAsync(new IOException("Connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<IOException>(() => _handler.HandleAsync(command));
        }
    }
}
