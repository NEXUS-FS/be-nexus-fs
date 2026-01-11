using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Application.DTOs.FileOperations;
using be_nexus_fs.Controllers;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NexusFS.Tests.Controllers;

public class McpControllerTests
{
    private class TestProvider : Provider
    {
        public readonly Dictionary<string, string> Files = new();
        public bool ThrowOnDelete { get; set; }

        public TestProvider(string id) : base(id, "Local", new Dictionary<string, string>()) { }

        public override Task<string> ReadFileAsync(string filePath)
        {
            if (!Files.ContainsKey(filePath)) throw new FileNotFoundException(filePath);
            return Task.FromResult(Files[filePath]);
        }

        public override Task WriteFileAsync(string filePath, string content)
        {
            Files[filePath] = content;
            return Task.CompletedTask;
        }

        public override Task DeleteFileAsync(string filePath)
        {
            if (ThrowOnDelete) throw new FileNotFoundException(filePath);
            Files.Remove(filePath);
            return Task.CompletedTask;
        }

        public override Task<bool> TestConnectionAsync() => Task.FromResult(true);
        public override Task Initialize(Dictionary<string, string> config) => Task.CompletedTask;
        public override Task<List<string>> ListFilesAsync(string directoryPath, bool recursive) => Task.FromResult(new List<string>(Files.Keys));
        public override Task<FileMetadata> StatAsync(string path) => Task.FromResult(new FileMetadata { Path = path });
        public override Task MkdirAsync(string path, bool recursive = true) => Task.CompletedTask;
        public override Task CopyAsync(string sourcePath, string destinationPath)
        {
            if (Files.TryGetValue(sourcePath, out var content))
            {
                Files[destinationPath] = content;
            }
            return Task.CompletedTask;
        }
        public override Task MoveAsync(string sourcePath, string destinationPath)
        {
            if (Files.TryGetValue(sourcePath, out var content))
            {
                Files.Remove(sourcePath);
                Files[destinationPath] = content;
            }
            return Task.CompletedTask;
        }
        public override Task<bool> ExistsAsync(string path) => Task.FromResult(Files.ContainsKey(path));
        public override Task<Stream> ReadStreamAsync(string filePath) => Task.FromResult<Stream>(new MemoryStream());
        public override Task WriteStreamAsync(string filePath, Stream content) => Task.CompletedTask;
    }

    private static UriRouter BuildRouter(out TestProvider provider)
    {
        var auditRepo = new Mock<Domain.Repositories.IAuditLogRepository>();
        var logger = new Logger(auditRepo.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var providerFactory = new ProviderFactory();
        var manager = new ProviderManager(providerFactory, logger, scopeFactory.Object, Array.Empty<IProviderObserver>());

        provider = new TestProvider("local-provider");
        var providersField = typeof(ProviderManager).GetField("_providers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        providersField!.SetValue(manager, new Dictionary<string, Provider> { { provider.ProviderId, provider } });

        return new UriRouter(manager, logger);
    }

    private static McpController BuildController(out TestProvider provider)
    {
        var router = BuildRouter(out provider);
        return new McpController(router, Mock.Of<ILogger<McpController>>());
    }

    [Fact]
    public async Task ReadFile_ReturnsOk_OnSuccess()
    {
        var controller = BuildController(out var provider);
        provider.Files["/tmp/a.txt"] = "content";

        var result = await controller.ReadFile(new McpReadFileRequest { Uri = "file:///tmp/a.txt" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<McpReadFileResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("content", payload.Content);
    }

    [Fact]
    public async Task WriteFile_ReturnsOk_OnSuccess()
    {
        var controller = BuildController(out var provider);

        var result = await controller.WriteFile(new McpWriteFileRequest { Uri = "file:///tmp/b.txt", Content = "data" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<McpWriteFileResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal(4, payload.BytesWritten);
        Assert.Equal("data", provider.Files["/tmp/b.txt"]);
    }

    [Fact]
    public async Task Exists_ReturnsOk_OnSuccess()
    {
        var controller = BuildController(out var provider);
        provider.Files["/tmp/c.txt"] = "x";

        var result = await controller.Exists(new McpExistsRequest { Uri = "file:///tmp/c.txt" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<McpExistsResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.True(payload.Exists);
    }

    [Fact]
    public async Task ListDirectory_ReturnsOk_OnSuccess()
    {
        var controller = BuildController(out var provider);
        provider.Files["/tmp/a.txt"] = "1";
        provider.Files["/tmp/b.txt"] = "2";

        var result = await controller.ListDirectory(new McpListDirectoryRequest { Uri = "file:///tmp", Recursive = true });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<McpListDirectoryResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal(2, payload.Count);
    }

    [Fact]
    public async Task DeleteFile_ReturnsOk_OnSuccess()
    {
        var controller = BuildController(out var provider);
        provider.Files["/tmp/z.txt"] = "z";

        var result = await controller.DeleteFile(new McpDeleteFileRequest { Uri = "file:///tmp/z.txt" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<McpDeleteFileResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.False(provider.Files.ContainsKey("/tmp/z.txt"));
    }

    [Fact]
    public async Task ReadFile_Returns500_OnInvalidUri()
    {
        var controller = BuildController(out _);

        var result = await controller.ReadFile(new McpReadFileRequest { Uri = "not a uri" });

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, obj.StatusCode);
        var payload = Assert.IsType<McpReadFileResponse>(obj.Value);
        Assert.False(payload.Success);
        Assert.Contains("Invalid URI format", payload.Error);
    }

    [Fact]
    public async Task DeleteFile_Returns500_WhenProviderMissing()
    {
        var controller = BuildController(out var provider);
        provider.ThrowOnDelete = false;

        var result = await controller.DeleteFile(new McpDeleteFileRequest { Uri = "file:///tmp/missing.txt" });

        // Provider exists, but file missing does not throw; simulate provider missing by using unsupported scheme
        var badResult = await controller.DeleteFile(new McpDeleteFileRequest { Uri = "s3://bucket/key" });

        var obj = Assert.IsType<ObjectResult>(badResult.Result);
        Assert.Equal(500, obj.StatusCode);
        var payload = Assert.IsType<McpDeleteFileResponse>(obj.Value);
        Assert.False(payload.Success);
    }

    [Fact]
    public async Task Exists_Returns500_OnUnsupportedScheme()
    {
        var controller = BuildController(out _);

        var result = await controller.Exists(new McpExistsRequest { Uri = "custom://foo/bar" });

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, obj.StatusCode);
        var payload = Assert.IsType<McpExistsResponse>(obj.Value);
        Assert.False(payload.Success);
    }
}
