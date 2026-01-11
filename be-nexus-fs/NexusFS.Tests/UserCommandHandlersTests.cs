using System.Threading.Tasks;
using Application.DTOs.User;
using Application.UseCases.Users.Commands;
using Application.UseCases.Users.CommandsHandler;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace NexusFS.Tests
{
    public class UserCommandHandlersTests
    {
        [Fact]
        public async Task CreateUserHandler_Success_ReturnsDto()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.UsernameExistsAsync("alice", null)).ReturnsAsync(false);
            repo.Setup(r => r.EmailExistsAsync("alice@example.com", null)).ReturnsAsync(false);
            repo.Setup(r => r.AddAsync(It.IsAny<UserEntity>())).ReturnsAsync((UserEntity e) =>
            {
                e.Id = "u1";
                e.IsActive = true;
                e.CreatedAt = System.DateTime.UtcNow;
                return e;
            });

            var handler = new CreateUserHandler(repo.Object);
            var cmd = new CreateUserCommand
            {
                CreateUserDto = new CreateUserDto
                {
                    Username = "alice",
                    Email = "alice@example.com",
                    Password = "P@ssw0rd",
                    Provider = "Basic",
                    Role = "User"
                }
            };

            var dto = await handler.HandleAsync(cmd);
            dto.Id.Should().Be("u1");
            dto.Username.Should().Be("alice");
            dto.Email.Should().Be("alice@example.com");
            dto.Role.Should().Be("User");
        }

        [Fact]
        public async Task CreateUserHandler_MissingUsername_Throws()
        {
            var repo = new Mock<IUserRepository>();
            var handler = new CreateUserHandler(repo.Object);
            var cmd = new CreateUserCommand { CreateUserDto = new CreateUserDto { Username = "", Email = "a@b" } };
            await FluentActions.Invoking(() => handler.HandleAsync(cmd))
                .Should().ThrowAsync<System.ArgumentException>().WithMessage("Username is required");
        }

        [Fact]
        public async Task CreateUserHandler_MissingEmail_Throws()
        {
            var repo = new Mock<IUserRepository>();
            var handler = new CreateUserHandler(repo.Object);
            var cmd = new CreateUserCommand { CreateUserDto = new CreateUserDto { Username = "alice", Email = "" } };
            await FluentActions.Invoking(() => handler.HandleAsync(cmd))
                .Should().ThrowAsync<System.ArgumentException>().WithMessage("Email is required");
        }

        [Fact]
        public async Task CreateUserHandler_DuplicateUsername_Throws()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.UsernameExistsAsync("alice", null)).ReturnsAsync(true);
            var handler = new CreateUserHandler(repo.Object);
            var cmd = new CreateUserCommand { CreateUserDto = new CreateUserDto { Username = "alice", Email = "a@b" } };
            await FluentActions.Invoking(() => handler.HandleAsync(cmd))
                .Should().ThrowAsync<System.InvalidOperationException>()
                .WithMessage("Username 'alice' already exists");
        }

        [Fact]
        public async Task CreateUserHandler_DuplicateEmail_Throws()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.UsernameExistsAsync("alice", null)).ReturnsAsync(false);
            repo.Setup(r => r.EmailExistsAsync("a@b", null)).ReturnsAsync(true);
            var handler = new CreateUserHandler(repo.Object);
            var cmd = new CreateUserCommand { CreateUserDto = new CreateUserDto { Username = "alice", Email = "a@b" } };
            await FluentActions.Invoking(() => handler.HandleAsync(cmd))
                .Should().ThrowAsync<System.InvalidOperationException>()
                .WithMessage("Email 'a@b' already exists");
        }

        [Fact]
        public async Task UpdateUserHandler_NotFound_Throws()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetByIdAsync("u1")).ReturnsAsync((UserEntity?)null);
            var handler = new UpdateUserHandler(repo.Object);
            var cmd = new UpdateUserCommand { UserId = "u1", UpdateUserDto = new UpdateUserDto { Username = "a", Email = "a@b" } };
            await FluentActions.Invoking(() => handler.HandleAsync(cmd))
                .Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>()
                .WithMessage("User with ID 'u1' not found");
        }

        [Fact]
        public async Task UpdateUserHandler_DuplicateUsername_Throws()
        {
            var existing = new UserEntity { Id = "u1", Username = "old", Email = "old@e", Role = "User", Provider = "Basic", IsActive = true, CreatedAt = System.DateTime.UtcNow };
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetByIdAsync("u1")).ReturnsAsync(existing);
            repo.Setup(r => r.UsernameExistsAsync("new", "u1")).ReturnsAsync(true);

            var handler = new UpdateUserHandler(repo.Object);
            var cmd = new UpdateUserCommand { UserId = "u1", UpdateUserDto = new UpdateUserDto { Username = "new", Email = "old@e" } };
            await FluentActions.Invoking(() => handler.HandleAsync(cmd))
                .Should().ThrowAsync<System.InvalidOperationException>()
                .WithMessage("Username 'new' already exists");
        }

        [Fact]
        public async Task UpdateUserHandler_DuplicateEmail_Throws()
        {
            var existing = new UserEntity { Id = "u1", Username = "old", Email = "old@e", Role = "User", Provider = "Basic", IsActive = true, CreatedAt = System.DateTime.UtcNow };
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetByIdAsync("u1")).ReturnsAsync(existing);
            repo.Setup(r => r.EmailExistsAsync("new@e", "u1")).ReturnsAsync(true);

            var handler = new UpdateUserHandler(repo.Object);
            var cmd = new UpdateUserCommand { UserId = "u1", UpdateUserDto = new UpdateUserDto { Username = "old", Email = "new@e" } };
            await FluentActions.Invoking(() => handler.HandleAsync(cmd))
                .Should().ThrowAsync<System.InvalidOperationException>()
                .WithMessage("Email 'new@e' already exists");
        }

        [Fact]
        public async Task UpdateUserHandler_Success_UpdatesAndCallsRepository()
        {
            var existing = new UserEntity { Id = "u1", Username = "old", Email = "old@e", Role = "User", Provider = "Basic", IsActive = true, CreatedAt = System.DateTime.UtcNow };
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetByIdAsync("u1")).ReturnsAsync(existing);
            repo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);
            repo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);
            repo.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask).Verifiable();

            var handler = new UpdateUserHandler(repo.Object);
            var cmd = new UpdateUserCommand { UserId = "u1", UpdateUserDto = new UpdateUserDto { Username = "new", Email = "new@e", Role = "Admin", IsActive = false, NewPassword = "newpass" } };

            await handler.HandleAsync(cmd);

            existing.Username.Should().Be("new");
            existing.Email.Should().Be("new@e");
            existing.Role.Should().Be("Admin");
            existing.IsActive.Should().BeFalse();
            existing.PasswordHash.Should().Be("newpass");
            repo.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task DeleteUserHandler_CallsRepositoryDelete()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.DeleteAsync("u1")).Returns(Task.CompletedTask).Verifiable();
            var handler = new DeleteUserHandler(repo.Object);
            await handler.HandleAsync(new DeleteUserCommand { UserId = "u1" });
            repo.Verify(r => r.DeleteAsync("u1"), Times.Once);
        }
    }
}
