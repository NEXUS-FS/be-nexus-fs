using Application.DTOs.FileOperations;
using Application.UseCases.FileOperations.Commands;
using Application.UseCases.FileOperations.CommandsHandler;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace NexusFS.Tests.FileOperations;

public class CopyMoveHandlerTests
{
    [Fact]
    public async Task CopyFile_ReturnsError_WhenProviderMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(false);

        var handler = new CopyFileHandler(repo.Object, Mock.Of<ILogger<CopyFileHandler>>());
        var command = BuildCopyCommand();

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
        repo.Verify(r => r.CopyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CopyFile_ReturnsSuccess_WhenRepositoryCompletes()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.CopyAsync("p1", "/source.txt", "/dest.txt")).Returns(Task.CompletedTask);

        var handler = new CopyFileHandler(repo.Object, Mock.Of<ILogger<CopyFileHandler>>());
        var command = BuildCopyCommand();

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        result.SourcePath.Should().Be("/source.txt");
        result.DestinationPath.Should().Be("/dest.txt");
        repo.Verify(r => r.CopyAsync("p1", "/source.txt", "/dest.txt"), Times.Once);
    }

    [Fact]
    public async Task CopyFile_ReturnsError_WhenSourceMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.CopyAsync("p1", "/source.txt", "/dest.txt"))
            .ThrowsAsync(new FileNotFoundException("missing"));

        var handler = new CopyFileHandler(repo.Object, Mock.Of<ILogger<CopyFileHandler>>());
        var command = BuildCopyCommand();

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Source file not found");
    }

    [Fact]
    public async Task CopyFile_RethrowsUnexpectedErrors()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.CopyAsync("p1", "/source.txt", "/dest.txt"))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var handler = new CopyFileHandler(repo.Object, Mock.Of<ILogger<CopyFileHandler>>());
        var command = BuildCopyCommand();

        var act = () => handler.HandleAsync(command);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MoveFile_ReturnsError_WhenProviderMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(false);

        var handler = new MoveFileHandler(repo.Object, Mock.Of<ILogger<MoveFileHandler>>());
        var command = BuildMoveCommand();

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
        repo.Verify(r => r.MoveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task MoveFile_ReturnsSuccess_WhenRepositoryCompletes()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.MoveAsync("p1", "/source.txt", "/dest.txt")).Returns(Task.CompletedTask);

        var handler = new MoveFileHandler(repo.Object, Mock.Of<ILogger<MoveFileHandler>>());
        var command = BuildMoveCommand();

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeTrue();
        result.SourcePath.Should().Be("/source.txt");
        result.DestinationPath.Should().Be("/dest.txt");
        repo.Verify(r => r.MoveAsync("p1", "/source.txt", "/dest.txt"), Times.Once);
    }

    [Fact]
    public async Task MoveFile_ReturnsError_WhenSourceMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.MoveAsync("p1", "/source.txt", "/dest.txt"))
            .ThrowsAsync(new FileNotFoundException("missing"));

        var handler = new MoveFileHandler(repo.Object, Mock.Of<ILogger<MoveFileHandler>>());
        var command = BuildMoveCommand();

        var result = await handler.HandleAsync(command);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Source file not found");
    }

    [Fact]
    public async Task MoveFile_RethrowsUnexpectedErrors()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.MoveAsync("p1", "/source.txt", "/dest.txt"))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var handler = new MoveFileHandler(repo.Object, Mock.Of<ILogger<MoveFileHandler>>());
        var command = BuildMoveCommand();

        var act = () => handler.HandleAsync(command);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static CopyFileCommand BuildCopyCommand() => new()
    {
        Request = new CopyFileRequest
        {
            ProviderId = "p1",
            SourcePath = "/source.txt",
            DestinationPath = "/dest.txt",
            UserId = "u1"
        }
    };

    private static MoveFileCommand BuildMoveCommand() => new()
    {
        Request = new MoveFileRequest
        {
            ProviderId = "p1",
            SourcePath = "/source.txt",
            DestinationPath = "/dest.txt",
            UserId = "u1"
        }
    };
}
