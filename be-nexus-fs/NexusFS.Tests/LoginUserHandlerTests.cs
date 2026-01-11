using System.Threading.Tasks;
using Application.Common.Security;
using Application.DTOs.Auth;
using Application.UseCases.Users.Commands;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace NexusFS.Tests
{
    public class LoginUserHandlerTests
    {
        [Fact]
        public async Task HandleAsync_ValidCredentials_ReturnsTokensAndUser()
        {
            var user = new UserEntity
            {
                Id = "u1",
                Username = "alice",
                Email = "alice@example.com",
                Role = "User",
                Provider = "Basic",
                IsActive = true,
                CreatedAt = System.DateTime.UtcNow
            };

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.ValidateCredentialsAsync("alice", "pass")).ReturnsAsync(user);
            repo.Setup(r => r.UpdateLastLoginAsync("u1")).Returns(Task.CompletedTask).Verifiable();

            var jwt = new Mock<IJwtTokenService>();
            jwt.Setup(j => j.GenerateAccessToken(user)).Returns("access-token");
            jwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");

            var handler = new LoginUserHandler(repo.Object, jwt.Object);
            var cmd = new LoginUserCommand { loginRequest = new LoginRequest { Username = "alice", Password = "pass" } };

            var resp = await handler.HandleAsync(cmd);

            resp.AccessToken.Should().Be("access-token");
            resp.RefreshToken.Should().Be("refresh-token");
            resp.User.Username.Should().Be("alice");

            repo.Verify(r => r.UpdateLastLoginAsync("u1"), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_InvalidCredentials_ThrowsUnauthorized()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.ValidateCredentialsAsync("alice", "bad")).ReturnsAsync((UserEntity?)null);
            var jwt = new Mock<IJwtTokenService>();

            var handler = new LoginUserHandler(repo.Object, jwt.Object);
            var cmd = new LoginUserCommand { loginRequest = new LoginRequest { Username = "alice", Password = "bad" } };

            await FluentActions.Invoking(() => handler.HandleAsync(cmd))
                .Should().ThrowAsync<System.UnauthorizedAccessException>()
                .WithMessage("Invalid username or password.");
        }
    }
}
