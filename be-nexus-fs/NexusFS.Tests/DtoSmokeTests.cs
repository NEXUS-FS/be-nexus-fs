using System;
using System.Collections.Generic;
using Application.DTOs;
using Application.DTOs.Auth;
using Application.DTOs.Common;
using Application.DTOs.FileOperations;
using Application.DTOs.User;
using Xunit;

namespace NexusFS.Tests;

public class DtoSmokeTests
{
    [Fact]
    public void AuthDtos_CanBeConstructed()
    {
        var changeRole = new ChangeRoleRequest { NewRole = "Admin" };
        Assert.Equal("Admin", changeRole.NewRole);

        var google = new GoogleAuthRequest { IdToken = "idtoken" };
        Assert.Equal("idtoken", google.IdToken);

        var refresh = new RefreshTokenRequest { RefreshToken = "rt" };
        Assert.Equal("rt", refresh.RefreshToken);

        var register = new RegisterRequest { Username = "u", Email = "e", Password = "p" };
        Assert.Equal("u", register.Username);

        var update = new UpdateUserRequest { Username = "u2", Email = "e2" };
        Assert.Equal("u2", update.Username);

        var userResponse = new UserResponse { Id = "id", Username = "u", Email = "e" };
        Assert.Equal("id", userResponse.Id);
    }

    [Fact]
    public void FileOperationDtos_CanBeConstructed()
    {
        var copyReq = new CopyFileRequest { ProviderId = "p", SourcePath = "/a", DestinationPath = "/b", UserId = "u" };
        Assert.Equal("/b", copyReq.DestinationPath);

        var copyResp = new CopyFileResponse { Success = true, Message = "ok" };
        Assert.True(copyResp.Success);

        var moveReq = new MoveFileRequest { ProviderId = "p", SourcePath = "/a", DestinationPath = "/c", UserId = "u" };
        Assert.Equal("/c", moveReq.DestinationPath);

        var moveResp = new MoveFileResponse { Success = true, Message = "done" };
        Assert.True(moveResp.Success);

        var listResp = new ListFilesResponse
        {
            Success = true,
            Files = new List<string> { "a", "b" },
            DirectoryPath = "/"
        };
        Assert.Equal(2, listResp.Files.Count);

        var existsResp = new ExistsResponse { Success = true, Exists = true, Path = "/a" };
        Assert.True(existsResp.Exists);

        var statReq = new StatFileRequest { ProviderId = "p", Path = "/x", UserId = "u" };
        Assert.Equal("/x", statReq.Path);
    }

    [Fact]
    public void MiscDtos_CanBeConstructed()
    {
        var metrics = new MetricsData { MetricName = "files.count", Value = 1.2, Unit = "count" };
        Assert.Equal("files.count", metrics.MetricName);

        var perms = new UserPermissionsDto { Username = "u", Permissions = new List<string> { "read", "write" } };
        Assert.Contains("write", perms.Permissions);

        var accessReq = new AccessControlRequest();
        Assert.NotNull(accessReq);

        var accessResp = new AccessControlResponse();
        Assert.NotNull(accessResp);

        var status = new ProviderStatusResponse();
        Assert.NotNull(status);

        var paged = new PagedResponse<string>
        {
            Data = new List<string> { "a" },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1
        };
        Assert.Single(paged.Data);
    }
}
