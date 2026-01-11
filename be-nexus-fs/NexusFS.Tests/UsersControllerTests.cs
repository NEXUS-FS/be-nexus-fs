using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using be_nexus_fs.Controllers;
using Application.DTOs.User;
using Application.UseCases.Users.Queries;
using Domain.Repositories;
using Domain.Entities;

namespace NexusFS.Tests
{
    public class UsersControllerTests
    {
        [Fact]
        public async Task GetUserById_ReturnsExpectedUser()
        {
            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetByIdAsync("test-id")).ReturnsAsync(new UserEntity { Id = "test-id", Username = "testuser", Email = "a@b.com", Role = "User", Provider = "Basic", IsActive = true, CreatedAt = DateTime.UtcNow });
            var handler = new GetUserByIdHandler(repo.Object);
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<UsersController>>().Object;
            var controller = new UsersController(
                null, null, null, null,
                handler,
                null, null, null,
                logger
            );
            var userId = "test-id";

            var result = await controller.GetUserById(userId);

            var action = Assert.IsType<ActionResult<UserDto>>(result);
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var payload = Assert.IsType<UserDto>(ok.Value);
            Assert.Equal("test-id", payload.Id);
        }
    }
}
