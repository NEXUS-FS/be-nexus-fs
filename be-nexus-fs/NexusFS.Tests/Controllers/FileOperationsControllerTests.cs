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
    public async Task ReadFile_ReturnsNotFound_WhenFileMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.ReadFileAsync("p1", "/nope.txt")).ThrowsAsync(new FileNotFoundException("nope"));

        var controller = BuildController(repo);
        var request = new ReadFileRequest { ProviderId = "p1", FilePath = "/nope.txt", UserId = "u" };

        var result = await controller.ReadFile(request);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task ReadFile_Returns500_OnUnexpectedError()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.ReadFileAsync("p1", "/boom.txt")).ThrowsAsync(new InvalidOperationException("boom"));

        var controller = BuildController(repo);
        var request = new ReadFileRequest { ProviderId = "p1", FilePath = "/boom.txt", UserId = "u" };

        var result = await controller.ReadFile(request);

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, obj.StatusCode);
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

    [Fact]
    public async Task CheckExists_ReturnsBadRequest_WhenPathMissing()
    {
        var controller = BuildController(new Mock<IFileOperationRepository>());

        var result = await controller.CheckExists("p1", "", "u");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task DownloadStream_ReturnsFileResult_OnSuccess()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.ReadStreamAsync("p1", "/file.txt")).ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

        var controller = BuildController(repo);

        var result = await controller.DownloadStream("p1", "/file.txt");

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/octet-stream", file.ContentType);
        Assert.Equal("file.txt", file.FileDownloadName);
    }

    [Fact]
    public async Task CopyFile_ReturnsNotFound_WhenProviderMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(false);

        var controller = BuildController(repo);
        var request = new CopyFileRequest { ProviderId = "p1", SourcePath = "/a.txt", DestinationPath = "/b.txt", UserId = "u" };

        var result = await controller.CopyFile(request);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CopyFile_ReturnsNotFound_WhenSourceMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.CopyAsync("p1", "/missing.txt", "/b.txt"))
            .ThrowsAsync(new FileNotFoundException("missing.txt"));

        var controller = BuildController(repo);
        var request = new CopyFileRequest { ProviderId = "p1", SourcePath = "/missing.txt", DestinationPath = "/b.txt", UserId = "u" };

        var result = await controller.CopyFile(request);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task MoveFile_ReturnsNotFound_WhenProviderMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(false);

        var controller = BuildController(repo);
        var request = new MoveFileRequest { ProviderId = "p1", SourcePath = "/a.txt", DestinationPath = "/b.txt", UserId = "u" };

        var result = await controller.MoveFile(request);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task MoveFile_ReturnsNotFound_WhenSourceMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.MoveAsync("p1", "/missing.txt", "/b.txt"))
            .ThrowsAsync(new FileNotFoundException("missing.txt"));

        var controller = BuildController(repo);
        var request = new MoveFileRequest { ProviderId = "p1", SourcePath = "/missing.txt", DestinationPath = "/b.txt", UserId = "u" };

        var result = await controller.MoveFile(request);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CheckExists_ReturnsNotFound_WhenProviderMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(false);

        var controller = BuildController(repo);

        var result = await controller.CheckExists("p1", "/file.txt", "u");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task WriteFile_ReturnsNotFound_WhenProviderMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(false);

        var controller = BuildController(repo);
        var request = new WriteFileRequest { ProviderId = "p1", FilePath = "/a.txt", Content = "data", UserId = "u" };

        var result = await controller.WriteFile(request);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task ListFiles_ReturnsOk_WhenSuccess()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.ListFilesAsync("p1", "/tmp", true))
            .ReturnsAsync(new List<string> { "a", "b" });

        var controller = BuildController(repo);

        var result = await controller.ListFiles("p1", "/tmp", true, "u");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ListFilesCommandResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal(2, payload.Files.Count);
    }

    [Fact]
    public async Task DeleteFile_ReturnsOk_WhenSuccess()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.DeleteFileAsync("p1", "/a.txt")).Returns(Task.CompletedTask);

        var controller = BuildController(repo);
        var request = new DeleteFileRequest { ProviderId = "p1", FilePath = "/a.txt", UserId = "u" };

        var result = await controller.DeleteFile(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<DeleteFileCommandResponse>(ok.Value);
        Assert.True(payload.Success);
    }

    [Fact]
    public async Task CreateDirectory_ReturnsOk_WhenSuccess()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.MkdirAsync("p1", "/dir", true)).Returns(Task.CompletedTask);

        var controller = BuildController(repo);
        var request = new MkdirRequest { ProviderId = "p1", Path = "/dir", Recursive = true, UserId = "u" };

        var result = await controller.CreateDirectory(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<MkdirResponse>(ok.Value);
        Assert.True(payload.Success);
    }

    [Fact]
    public async Task CreateDirectory_ReturnsNotFound_WhenProviderMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(false);

        var controller = BuildController(repo);
        var request = new MkdirRequest { ProviderId = "p1", Path = "/dir", Recursive = true, UserId = "u" };

        var result = await controller.CreateDirectory(request);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task StatFile_ReturnsOk_WhenSuccess()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.StatAsync("p1", "/file.txt"))
            .ReturnsAsync(new Domain.Models.FileMetadata { Path = "/file.txt", Size = 10 });

        var controller = BuildController(repo);

        var request = new StatFileRequest { ProviderId = "p1", Path = "/file.txt", UserId = "u" };
        var result = await controller.StatFile(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<StatFileResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("/file.txt", payload.Metadata.Path);
    }

    [Fact]
    public async Task StatFile_ReturnsNotFound_WhenProviderMissing()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(false);

        var controller = BuildController(repo);

        var request = new StatFileRequest { ProviderId = "p1", Path = "/file.txt", UserId = "u" };
        var result = await controller.StatFile(request);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Exists_ReturnsOk_WhenProviderExists()
    {
        var repo = new Mock<IFileOperationRepository>();
        repo.Setup(r => r.ProviderExistsAsync("p1")).ReturnsAsync(true);
        repo.Setup(r => r.ExistsAsync("p1", "/file.txt")).ReturnsAsync(true);

        var controller = BuildController(repo);

        var result = await controller.CheckExists("p1", "/file.txt", "u");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ExistsResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.True(payload.Exists);
    }
}
