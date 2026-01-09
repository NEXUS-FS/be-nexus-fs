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
    public class MkdirHandlerTests
    {
        private readonly Mock<IFileOperationRepository> _repositoryMock;
        private readonly Mock<ILogger<MkdirHandler>> _loggerMock;
        private readonly MkdirHandler _handler;

        public MkdirHandlerTests()
        {
            _repositoryMock = new Mock<IFileOperationRepository>();
            _loggerMock = new Mock<ILogger<MkdirHandler>>();
            _handler = new MkdirHandler(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WithValidPath_ShouldCreateDirectory()
        {
            // Arrange
            var command = new MkdirCommand
            {
                Request = new MkdirRequest
                {
                    ProviderId = "test-provider",
                    Path = "/test/newdir",
                    Recursive = false,
                    UserId = "test-user"
                }
            };

            _repositoryMock.Setup(x => x.ProviderExistsAsync(command.Request.ProviderId))
                .ReturnsAsync(true);
            _repositoryMock.Setup(x => x.MkdirAsync(command.Request.ProviderId, command.Request.Path, command.Request.Recursive))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Path.Should().Be("/test/newdir");
            _repositoryMock.Verify(x => x.MkdirAsync(command.Request.ProviderId, command.Request.Path, command.Request.Recursive), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithRecursiveTrue_ShouldCreateNestedDirectories()
        {
            // Arrange
            var command = new MkdirCommand
            {
                Request = new MkdirRequest
                {
                    ProviderId = "test-provider",
                    Path = "/test/nested/deep/directory",
                    Recursive = true,
                    UserId = "test-user"
                }
            };

            _repositoryMock.Setup(x => x.ProviderExistsAsync(command.Request.ProviderId))
                .ReturnsAsync(true);
            _repositoryMock.Setup(x => x.MkdirAsync(command.Request.ProviderId, command.Request.Path, command.Request.Recursive))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            _repositoryMock.Verify(x => x.MkdirAsync(command.Request.ProviderId, command.Request.Path, true), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithRepositoryError_ShouldReturnFailure()
        {
            // Arrange
            var command = new MkdirCommand
            {
                Request = new MkdirRequest
                {
                    ProviderId = "test-provider",
                    Path = "/test/newdir",
                    Recursive = false,
                    UserId = "test-user"
                }
            };

            _repositoryMock.Setup(x => x.ProviderExistsAsync(command.Request.ProviderId))
                .ReturnsAsync(true);
            _repositoryMock.Setup(x => x.MkdirAsync(command.Request.ProviderId, command.Request.Path, command.Request.Recursive))
                .ThrowsAsync(new IOException("Failed to create directory"));

            // Act & Assert
            await Assert.ThrowsAsync<IOException>(() => _handler.HandleAsync(command));
        }
    }
}
