using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using be_nexus_fs.Controllers;
using Application.DTOs.FileOperations;
using Application.UseCases.FileOperations.Commands;
using Application.UseCases.FileOperations.CommandsHandler;
using Domain.Repositories;

namespace NexusFS.Tests
{
    public class FileOperationsControllerTests
    {
        private FileOperationsController CreateController(Mock<IFileOperationRepository> repoMock)
        {
            var readHandler = new ReadFileHandler(repoMock.Object, new Mock<Microsoft.Extensions.Logging.ILogger<ReadFileHandler>>().Object);
            var writeHandler = new WriteFileHandler(repoMock.Object, new Mock<Microsoft.Extensions.Logging.ILogger<WriteFileHandler>>().Object);
            var deleteHandler = new DeleteFileHandler(repoMock.Object, new Mock<Microsoft.Extensions.Logging.ILogger<DeleteFileHandler>>().Object);
            var listHandler = new ListFilesHandler(repoMock.Object, new Mock<Microsoft.Extensions.Logging.ILogger<ListFilesHandler>>().Object);
            var statHandler = new StatFileHandler(repoMock.Object, new Mock<Microsoft.Extensions.Logging.ILogger<StatFileHandler>>().Object);
            var mkdirHandler = new MkdirHandler(repoMock.Object, new Mock<Microsoft.Extensions.Logging.ILogger<MkdirHandler>>().Object);
            var copyHandler = new CopyFileHandler(repoMock.Object, new Mock<Microsoft.Extensions.Logging.ILogger<CopyFileHandler>>().Object);
            var moveHandler = new MoveFileHandler(repoMock.Object, new Mock<Microsoft.Extensions.Logging.ILogger<MoveFileHandler>>().Object);
            var existsHandler = new ExistsHandler(repoMock.Object, new Mock<Microsoft.Extensions.Logging.ILogger<ExistsHandler>>().Object);
            var controllerLogger = new Mock<Microsoft.Extensions.Logging.ILogger<FileOperationsController>>().Object;

            return new FileOperationsController(
                readHandler,
                writeHandler,
                deleteHandler,
                listHandler,
                statHandler,
                mkdirHandler,
                copyHandler,
                moveHandler,
                existsHandler,
                repoMock.Object,
                controllerLogger
            );
        }

        [Fact]
        public async Task ReadFile_ReturnsExpectedResponse()
        {
            var repoMock = new Mock<IFileOperationRepository>();
            repoMock.Setup(r => r.ProviderExistsAsync("pid")).ReturnsAsync(true);
            repoMock.Setup(r => r.ReadFileAsync("pid", "f.txt")).ReturnsAsync("file content");
            var controller = CreateController(repoMock);
            var request = new ReadFileRequest { ProviderId = "pid", FilePath = "f.txt", UserId = "uid" };

            var result = await controller.ReadFile(request);
            var action = Assert.IsType<ActionResult<ReadFileCommandResponse>>(result);
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var payload = Assert.IsType<ReadFileCommandResponse>(ok.Value);
            Assert.Equal("file content", payload.Content);
        }

        [Fact]
        public async Task WriteFile_ReturnsExpectedResponse()
        {
            var repoMock = new Mock<IFileOperationRepository>();
            repoMock.Setup(r => r.ProviderExistsAsync("pid")).ReturnsAsync(true);
            repoMock.Setup(r => r.WriteFileAsync("pid", "f.txt", "abc")).Returns(Task.CompletedTask);
            var controller = CreateController(repoMock);
            var request = new WriteFileRequest { ProviderId = "pid", FilePath = "f.txt", Content = "abc", UserId = "uid" };

            var result = await controller.WriteFile(request);
            var action = Assert.IsType<ActionResult<WriteFileCommandResponse>>(result);
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var payload = Assert.IsType<WriteFileCommandResponse>(ok.Value);
            Assert.True(payload.Success);
        }

        [Fact]
        public async Task DeleteFile_ReturnsExpectedResponse()
        {
            var repoMock = new Mock<IFileOperationRepository>();
            repoMock.Setup(r => r.ProviderExistsAsync("pid")).ReturnsAsync(true);
            repoMock.Setup(r => r.DeleteFileAsync("pid", "f.txt")).Returns(Task.CompletedTask);
            var controller = CreateController(repoMock);
            var request = new DeleteFileRequest { ProviderId = "pid", FilePath = "f.txt", UserId = "uid" };

            var result = await controller.DeleteFile(request);
            var action = Assert.IsType<ActionResult<DeleteFileCommandResponse>>(result);
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var payload = Assert.IsType<DeleteFileCommandResponse>(ok.Value);
            Assert.True(payload.Success);
        }

        [Fact]
        public async Task ListFiles_ReturnsExpectedResponse()
        {
            var repoMock = new Mock<IFileOperationRepository>();
            repoMock.Setup(r => r.ProviderExistsAsync("pid")).ReturnsAsync(true);
            repoMock.Setup(r => r.ListFilesAsync("pid", "/", false)).ReturnsAsync(new List<string> { "a.txt", "b.txt" });
            var controller = CreateController(repoMock);

            var result = await controller.ListFiles("pid", "/", false, "uid");
            var action = Assert.IsType<ActionResult<ListFilesCommandResponse>>(result);
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var payload = Assert.IsType<ListFilesCommandResponse>(ok.Value);
            Assert.True(payload.Success);
            Assert.Contains("a.txt", payload.Files!);
        }
    }
}
