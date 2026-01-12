using Application.UseCases.FileOperations.CommandsHandler;
using Application.UseCases.FileOperations.Commands;
using Application.DTOs.FileOperations;
using Domain.Repositories;
using Domain.Models;
using Moq;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Logging;

namespace NexusFS.Tests.FileOperations
{
    public class StatFileHandlerTests
    {
        private readonly Mock<IFileOperationRepository> _repositoryMock;
        private readonly Mock<ILogger<StatFileHandler>> _loggerMock;
        private readonly StatFileHandler _handler;

        public StatFileHandlerTests()
        {
            _repositoryMock = new Mock<IFileOperationRepository>();
            _loggerMock = new Mock<ILogger<StatFileHandler>>();
            _handler = new StatFileHandler(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WithValidPath_ShouldReturnMetadata()
        {
            // Arrange
            var command = new StatFileCommand
            {
                Request = new StatFileRequest
                {
                    ProviderId = "test-provider",
                    Path = "/test/file.txt",
                    UserId = "test-user"
                }
            };

            var expectedMetadata = new FileMetadata
            {
                Name = "file.txt",
                Path = "/test/file.txt",
                Size = 1024,
                Created = DateTime.UtcNow.AddDays(-7),
                Modified = DateTime.UtcNow,
                IsDirectory = false,
                Exists = true,
                ContentType = "text/plain"
            };

            _repositoryMock.Setup(x => x.ProviderExistsAsync(command.Request.ProviderId))
                .ReturnsAsync(true);
            _repositoryMock.Setup(x => x.StatAsync(command.Request.ProviderId, command.Request.Path))
                .ReturnsAsync(expectedMetadata);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Metadata.Should().NotBeNull();
            result.Metadata.Name.Should().Be("file.txt");
            result.Metadata.Size.Should().Be(1024);
            result.Metadata.Exists.Should().BeTrue();
        }

        [Fact]
        public async Task HandleAsync_WithNonExistentFile_ShouldReturnMetadataWithExistsFalse()
        {
            // Arrange
            var command = new StatFileCommand
            {
                Request = new StatFileRequest
                {
                    ProviderId = "test-provider",
                    Path = "/nonexistent.txt",
                    UserId = "test-user"
                }
            };

            var expectedMetadata = new FileMetadata
            {
                Name = "nonexistent.txt",
                Path = "/nonexistent.txt",
                Exists = false
            };

            _repositoryMock.Setup(x => x.ProviderExistsAsync(command.Request.ProviderId))
                .ReturnsAsync(true);
            _repositoryMock.Setup(x => x.StatAsync(command.Request.ProviderId, command.Request.Path))
                .ReturnsAsync(expectedMetadata);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Metadata.Should().NotBeNull();
            result.Metadata.Exists.Should().BeFalse();
        }

        [Fact]
        public async Task HandleAsync_WithDirectory_ShouldReturnDirectoryMetadata()
        {
            // Arrange
            var command = new StatFileCommand
            {
                Request = new StatFileRequest
                {
                    ProviderId = "test-provider",
                    Path = "/test/directory",
                    UserId = "test-user"
                }
            };

            var expectedMetadata = new FileMetadata
            {
                Name = "directory",
                Path = "/test/directory",
                Size = 0,
                IsDirectory = true,
                Exists = true
            };

            _repositoryMock.Setup(x => x.ProviderExistsAsync(command.Request.ProviderId))
                .ReturnsAsync(true);
            _repositoryMock.Setup(x => x.StatAsync(command.Request.ProviderId, command.Request.Path))
                .ReturnsAsync(expectedMetadata);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Metadata.Should().NotBeNull();
            result.Metadata!.IsDirectory.Should().BeTrue();
            result.Metadata!.Size.Should().Be(0);
        }
    }
}
