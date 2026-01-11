using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Application.DTOs.FileOperations;
using Application.UseCases.FileOperations.Commands;
using Application.UseCases.FileOperations.CommandsHandler;
using be_nexus_fs.Controllers;
using Domain.Repositories;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NexusFS.Tests.Controllers;

public class FileOperationsControllerTests
{
    private static FileOperationsController BuildController(Mock<IFileOperationRepository> repo)
    {
        var read = new ReadFileHandler(repo.Object, Mock.Of<ILogger<ReadFileHandler>>());
        var write = new WriteFileHandler(repo.Object, Mock.Of<ILogger<WriteFileHandler>>());
        var delete = new DeleteFileHandler(repo.Object, Mock.Of<ILogger<DeleteFileHandler>>());
        var list = new ListFilesHandler(repo.Object, Mock.Of<ILogger<ListFilesHandler>>());
        var stat = new StatFileHandler(repo.Object, Mock.Of<ILogger<StatFileHandler>>());
        var mkdir = new MkdirHandler(repo.Object, Mock.Of<ILogger<MkdirHandler>>());
        var copy = new CopyFileHandler(repo.Object, Mock.Of<ILogger<CopyFileHandler>>());
        var move = new MoveFileHandler(repo.Object, Mock.Of<ILogger<MoveFileHandler>>());
        var exists = new ExistsHandler(repo.Object, Mock.Of<ILogger<ExistsHandler>>());

        return new FileOperationsController(
            read,
            write,
            delete,
            list,
            stat,
            mkdir,
            copy,
            move,
            exists,
            repo.Object,
            Mock.Of<ILogger<FileOperationsController>>());
    }

    [Fact]
    public async Task ReadFile_ReturnsOk_WhenSuccess()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.ReadFileAsync("p1", "/a.txt")).ReturnsAsync("hello");

        var controller = BuildController(repo);
        var request = new ReadFileRequest { ProviderId = "p1", FilePath = "/a.txt", UserId = "u" };

        var result = await controller.ReadFile(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ReadFileCommandResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("hello", payload.Content);
    }

    [Fact]
    public async Task ReadFile_ReturnsNotFound_WhenProviderMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("missing")).ReturnsAsync(false);

        var controller = BuildController(repo);
        var request = new ReadFileRequest { ProviderId = "missing", FilePath = "/a.txt", UserId = "u" };

        var result = await controller.ReadFile(request);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task ListFiles_ReturnsBadRequest_WhenProviderMissing()
    {
        var controller = BuildController(new Mock<IFileOperationRepository>());

        var result = await controller.ListFiles("", "/tmp", false, null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task WriteFile_ReturnsOk_WhenSuccess()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.WriteFileAsync("p1", "/a.txt", "data")).Returns(Task.CompletedTask);

        var controller = BuildController(repo);
        var request = new WriteFileRequest { ProviderId = "p1", FilePath = "/a.txt", Content = "data", UserId = "u" };

        var result = await controller.WriteFile(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<WriteFileCommandResponse>(ok.Value);
        Assert.True(payload.Success);
    }

    [Fact]
    public async Task DownloadStream_ReturnsNotFound_WhenProviderMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("missing")).ReturnsAsync(false);

        var controller = BuildController(repo);

        var result = await controller.DownloadStream("missing", "/file.txt");

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
