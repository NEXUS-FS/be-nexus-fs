using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.User;
using Application.DTOs.Common;
using Application.UseCases.Users.Queries;
using Domain.Entities;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace NexusFS.Tests
{
    public class UserQueryHandlersTests
    {
        [Fact]
        public async Task GetAllUsersHandler_ReturnsPagedDtos()
        {
            var users = new List<UserEntity>
            {
                new UserEntity { Id = "1", Username = "u1", Email = "u1@e", Role = "User", Provider = "Basic", IsActive = true, CreatedAt = System.DateTime.UtcNow },
                new UserEntity { Id = "2", Username = "u2", Email = "u2@e", Role = "Admin", Provider = "Basic", IsActive = true, CreatedAt = System.DateTime.UtcNow }
            };
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync(users);
            repo.Setup(r => r.GetTotalCountAsync(false)).ReturnsAsync(users.Count);

            var handler = new GetAllUsersHandler(repo.Object);
            var resp = await handler.HandleAsync(new GetAllUsersQuery { PageNumber = 1, PageSize = 10 });

            resp.TotalCount.Should().Be(2);
            resp.PageNumber.Should().Be(1);
            resp.PageSize.Should().Be(10);
            resp.Data.Should().HaveCount(2);
            resp.Data.Select(d => d.Username).Should().Contain(new[] {"u1","u2"});
        }

        [Fact]
        public async Task GetUserByEmailHandler_ReturnsDto_WhenFound()
        {
            var user = new UserEntity { Id = "1", Username = "u1", Email = "u1@e", Role = "User", Provider = "Basic", IsActive = true, CreatedAt = System.DateTime.UtcNow };
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetByEmailAsync("u1@e")).ReturnsAsync(user);

            var handler = new GetUserByEmailHandler(repo.Object);
            var dto = await handler.HandleAsync(new GetUserByEmailQuery { Email = "u1@e" });

            dto.Should().NotBeNull();
            dto!.Email.Should().Be("u1@e");
        }

        [Fact]
        public async Task GetUserByEmailHandler_ReturnsNull_WhenMissing()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetByEmailAsync("missing@e")).ReturnsAsync((UserEntity?)null);
            var handler = new GetUserByEmailHandler(repo.Object);
            var dto = await handler.HandleAsync(new GetUserByEmailQuery { Email = "missing@e" });
            dto.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByIdHandler_ReturnsDto_WhenFound()
        {
            var user = new UserEntity { Id = "1", Username = "u1", Email = "u1@e", Role = "User", Provider = "Basic", IsActive = true, CreatedAt = System.DateTime.UtcNow };
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(user);
            var handler = new GetUserByIdHandler(repo.Object);
            var dto = await handler.HandleAsync(new GetUserByIdQuery { UserId = "1" });
            dto.Should().NotBeNull();
            dto!.Id.Should().Be("1");
        }

        [Fact]
        public async Task GetUserByIdHandler_ReturnsNull_WhenMissing()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetByIdAsync("404")).ReturnsAsync((UserEntity?)null);
            var handler = new GetUserByIdHandler(repo.Object);
            var dto = await handler.HandleAsync(new GetUserByIdQuery { UserId = "404" });
            dto.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByUsernameHandler_ReturnsDto_WhenFound()
        {
            var user = new UserEntity { Id = "1", Username = "u1", Email = "u1@e", Role = "User", Provider = "Basic", IsActive = true, CreatedAt = System.DateTime.UtcNow };
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetByUsernameAsync("u1")).ReturnsAsync(user);
            var handler = new GetUserByUsernameHandler(repo.Object);
            var dto = await handler.HandleAsync(new GetUserByUsernameQuery { Username = "u1" });
            dto.Should().NotBeNull();
            dto!.Username.Should().Be("u1");
        }

        [Fact]
        public async Task GetUserByUsernameHandler_ReturnsNull_WhenMissing()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetByUsernameAsync("nouser")).ReturnsAsync((UserEntity?)null);
            var handler = new GetUserByUsernameHandler(repo.Object);
            var dto = await handler.HandleAsync(new GetUserByUsernameQuery { Username = "nouser" });
            dto.Should().BeNull();
        }
    }
}
