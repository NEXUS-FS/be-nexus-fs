using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace NexusFS.Tests.Repositories;

public class FileOperationRepositoryTests
{
    private class TestProvider : Provider
    {
        public string? LastPath { get; private set; }
        public string? LastContent { get; private set; }
        public Dictionary<string, string> Calls { get; } = new();

        public TestProvider(string id) : base(id, "Test", new Dictionary<string, string>()) { }

        public override Task<string> ReadFileAsync(string filePath)
        {
            LastPath = filePath;
            return Task.FromResult($"read:{filePath}");
        }

        public override Task WriteFileAsync(string filePath, string content)
        {
            LastPath = filePath;
            LastContent = content;
            return Task.CompletedTask;
        }

        public override Task DeleteFileAsync(string filePath)
        {
            LastPath = filePath;
            return Task.CompletedTask;
        }

        public override Task<bool> TestConnectionAsync() => Task.FromResult(true);

        public override Task Initialize(Dictionary<string, string> config) => Task.CompletedTask;

        public override Task<List<string>> ListFilesAsync(string directoryPath, bool recursive)
        {
            LastPath = directoryPath;
            Calls["recursive"] = recursive.ToString();
            return Task.FromResult(new List<string> { "a", "b" });
        }

        public override Task<FileMetadata> StatAsync(string path)
        {
            LastPath = path;
            return Task.FromResult(new FileMetadata { Path = path });
        }

        public override Task MkdirAsync(string path, bool recursive = true)
        {
            LastPath = path;
            Calls["recursive"] = recursive.ToString();
            return Task.CompletedTask;
        }

        public override Task CopyAsync(string sourcePath, string destinationPath)
        {
            Calls["copy"] = $"{sourcePath}->{destinationPath}";
            return Task.CompletedTask;
        }

        public override Task MoveAsync(string sourcePath, string destinationPath)
        {
            Calls["move"] = $"{sourcePath}->{destinationPath}";
            return Task.CompletedTask;
        }

        public override Task<bool> ExistsAsync(string path)
        {
            LastPath = path;
            return Task.FromResult(true);
        }

        public override Task<Stream> ReadStreamAsync(string filePath)
        {
            LastPath = filePath;
            return Task.FromResult<Stream>(new MemoryStream());
        }

        public override Task WriteStreamAsync(string filePath, Stream content)
        {
            LastPath = filePath;
            return Task.CompletedTask;
        }
    }

    private static ProviderManager BuildProviderManagerWith(params Provider[] providers)
    {
        var auditRepo = new Mock<Domain.Repositories.IAuditLogRepository>();
        var logger = new Logger(auditRepo.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var providerFactory = new ProviderFactory();
        var manager = new ProviderManager(providerFactory, logger, scopeFactory.Object, new List<IProviderObserver>());

        var providersField = typeof(ProviderManager).GetField("_providers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        providersField!.SetValue(manager, providers.ToDictionary(p => p.ProviderId, p => p));
        return manager;
    }

    [Fact]
    public async Task ReadFileAsync_DelegatesToProvider()
    {
        var provider = new TestProvider("p1");
        var repo = new FileOperationRepository(BuildProviderManagerWith(provider));

        var result = await repo.ReadFileAsync("p1", "/path/file.txt");

        result.Should().Be("read:/path/file.txt");
        provider.LastPath.Should().Be("/path/file.txt");
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenProviderMissing()
    {
        var repo = new FileOperationRepository(BuildProviderManagerWith());
        (await repo.ProviderExistsAsync("unknown")).Should().BeFalse();
    }

    [Fact]
    public async Task Methods_Throw_WhenProviderNotFound()
    {
        var repo = new FileOperationRepository(BuildProviderManagerWith());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => repo.ReadFileAsync("missing", "path"));
    }

    [Fact]
    public async Task WriteFileAsync_DelegatesToProvider()
    {
        var provider = new TestProvider("p2");
        var repo = new FileOperationRepository(BuildProviderManagerWith(provider));

        await repo.WriteFileAsync("p2", "/tmp/out.txt", "data");

        provider.LastPath.Should().Be("/tmp/out.txt");
        provider.LastContent.Should().Be("data");
    }

    [Fact]
    public async Task ListFilesAsync_UsesProvider()
    {
        var provider = new TestProvider("p3");
        var repo = new FileOperationRepository(BuildProviderManagerWith(provider));

        var files = await repo.ListFilesAsync("p3", "/dir", recursive: true);

        files.Should().Contain(new[] { "a", "b" });
        provider.LastPath.Should().Be("/dir");
        provider.Calls["recursive"].Should().Be("True");
    }
}
