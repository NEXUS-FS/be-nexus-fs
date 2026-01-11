using System.Collections.Generic;
using Domain.Repositories;
using FluentAssertions;
using Infrastructure.Services.Security;
using Moq;

namespace NexusFS.Tests;

public class ACLManagerTests
{
    private readonly Mock<IAccessControlRepository> _repositoryMock;

    public ACLManagerTests()
    {
        _repositoryMock = new Mock<IAccessControlRepository>(MockBehavior.Strict);
        _repositoryMock.Setup(r => r.GetAllPermissionsAsync())
            .ReturnsAsync(new Dictionary<string, List<string>>());
    }

    [Fact]
    public async Task GrantPermissionAsync_CachesAfterRepositoryAdd()
    {
        _repositoryMock.Setup(r => r.AddPermissionAsync("alice", "read")).ReturnsAsync(true);

        var manager = CreateManager();
        await manager.GrantPermissionAsync("alice", "read");

        var hasPermission = await manager.HasPermissionAsync("alice", "read");

        hasPermission.Should().BeTrue();
        _repositoryMock.Verify(r => r.HasPermissionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.AddPermissionAsync("alice", "read"), Times.Once);
    }

    [Fact]
    public async Task HasPermissionAsync_CacheMissFallsBackAndCachesResult()
    {
        _repositoryMock.Setup(r => r.HasPermissionAsync("bob", "write")).ReturnsAsync(true);

        var manager = CreateManager();

        var first = await manager.HasPermissionAsync("bob", "write");
        var second = await manager.HasPermissionAsync("bob", "write");

        first.Should().BeTrue();
        second.Should().BeTrue();
        _repositoryMock.Verify(r => r.HasPermissionAsync("bob", "write"), Times.Once);
    }

    [Fact]
    public async Task RefreshCacheAsync_ReloadsPermissionsFromRepository()
    {
        _repositoryMock.Reset();
        _repositoryMock.SetupSequence(r => r.GetAllPermissionsAsync())
            .ReturnsAsync(new Dictionary<string, List<string>>())
            .ReturnsAsync(new Dictionary<string, List<string>>
            {
                { "carol", new List<string> { "delete" } }
            });

        var manager = new ACLManager(_repositoryMock.Object);

        await manager.RefreshCacheAsync();
        var hasPermission = await manager.HasPermissionAsync("carol", "delete");

        hasPermission.Should().BeTrue();
        _repositoryMock.Verify(r => r.HasPermissionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RevokePermissionAsync_RemovesCachedPermission()
    {
        _repositoryMock.Setup(r => r.AddPermissionAsync("dave", "read")).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.RemovePermissionAsync("dave", "read")).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.HasPermissionAsync("dave", "read")).ReturnsAsync(false);

        var manager = CreateManager();
        await manager.GrantPermissionAsync("dave", "read");

        await manager.RevokePermissionAsync("dave", "read");
        var hasPermission = await manager.HasPermissionAsync("dave", "read");

        hasPermission.Should().BeFalse();
        _repositoryMock.Verify(r => r.HasPermissionAsync("dave", "read"), Times.Once);
    }

    [Fact]
    public async Task GrantPermissionAsync_ThrowsWhenRepositoryFails()
    {
        _repositoryMock.Setup(r => r.AddPermissionAsync("erin", "read")).ReturnsAsync(false);

        var manager = CreateManager();

        var act = () => manager.GrantPermissionAsync("erin", "read");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private ACLManager CreateManager() => new(_repositoryMock.Object);
}
