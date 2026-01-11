using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Common.Security;
using Application.DTOs.Auth;
using Application.DTOs.User;
using Application.UseCases.Users.Commands;
using Application.UseCases.Users.CommandsHandler;
using Application.UseCases.Users.Queries;
using be_nexus_fs.Controllers;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NexusFS.Tests;

public class UsersControllerTests
{
    private static UsersController BuildController(
        Mock<IUserRepository> userRepoMock,
        Mock<IJwtTokenService>? jwtMock = null)
    {
        var create = new CreateUserHandler(userRepoMock.Object);
        var update = new UpdateUserHandler(userRepoMock.Object);
        var delete = new DeleteUserHandler(userRepoMock.Object);
        var login = new LoginUserHandler(userRepoMock.Object, (jwtMock ?? new Mock<IJwtTokenService>()).Object);
        var getById = new GetUserByIdHandler(userRepoMock.Object);
        var getAll = new GetAllUsersHandler(userRepoMock.Object);
        var getByUsername = new GetUserByUsernameHandler(userRepoMock.Object);
        var getByEmail = new GetUserByEmailHandler(userRepoMock.Object);
        var logger = new Mock<ILogger<UsersController>>();

        return new UsersController(
            create,
            update,
            delete,
            login,
            getById,
            getAll,
            getByUsername,
            getByEmail,
            logger.Object);
    }

    [Fact]
    public async Task GetUserById_ReturnsExpectedUser()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync("test-id")).ReturnsAsync(new UserEntity
        {
            Id = "test-id",
            Username = "testuser",
            Email = "a@b.com",
            Role = "User",
            Provider = "Basic",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        var controller = BuildController(repo);

        var result = await controller.GetUserById("test-id");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<UserDto>(ok.Value);
        Assert.Equal("test-id", payload.Id);
    }

    [Fact]
    public async Task GetUserById_NotFound_Returns404()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((UserEntity?)null);
        var controller = BuildController(repo);

        var result = await controller.GetUserById("missing");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_ReturnsCreated()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        repo.Setup(r => r.AddAsync(It.IsAny<UserEntity>()))
            .ReturnsAsync(new UserEntity
            {
                Id = "new-id",
                Username = "alice",
                Email = "a@b.com",
                Role = "User",
                Provider = "Basic",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        var controller = BuildController(repo);

        var dto = new CreateUserDto { Username = "alice", Email = "a@b.com", Password = "pw", Role = "User", Provider = "Basic" };
        var result = await controller.CreateUser(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var payload = Assert.IsType<UserDto>(created.Value);
        Assert.Equal("new-id", payload.Id);
    }

    [Fact]
    public async Task CreateUser_BadRequest_OnValidationError()
    {
        var repo = new Mock<IUserRepository>();
        var controller = BuildController(repo);

        var dto = new CreateUserDto { Username = "", Email = "a@b.com", Password = "pw", Role = "User", Provider = "Basic" };
        var result = await controller.CreateUser(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateUser_NotFound_Returns404()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((UserEntity?)null);
        var controller = BuildController(repo);

        var result = await controller.UpdateUser("missing", new UpdateUserDto { Username = "u", Email = "e", Role = "User", IsActive = true });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUser_PropagatesNotFound()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.DeleteAsync("missing")).ThrowsAsync(new KeyNotFoundException("nf"));
        var controller = BuildController(repo);

        var result = await controller.DeleteUser("missing");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_OnInvalidCredentials()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((UserEntity?)null);

        var jwt = new Mock<IJwtTokenService>();
        var controller = BuildController(repo, jwt);

        var result = await controller.Login(new LoginUserCommand { loginRequest = new LoginRequest { Username = "u", Password = "p" } });

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_ReturnsOk_OnSuccess()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.ValidateCredentialsAsync("u", "p")).ReturnsAsync(new UserEntity
        {
            Id = "id",
            Username = "u",
            Email = "e",
            Role = "User",
            Provider = "Basic",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        repo.Setup(r => r.UpdateLastLoginAsync("id")).Returns(Task.CompletedTask);

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(j => j.GenerateAccessToken(It.IsAny<UserEntity>())).Returns("access");
        jwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh");

        var controller = BuildController(repo, jwt);

        var result = await controller.Login(new LoginUserCommand { loginRequest = new LoginRequest { Username = "u", Password = "p" } });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<LoginResponse>(ok.Value);
        Assert.Equal("access", payload.AccessToken);
        Assert.Equal("refresh", payload.RefreshToken);
    }
}
