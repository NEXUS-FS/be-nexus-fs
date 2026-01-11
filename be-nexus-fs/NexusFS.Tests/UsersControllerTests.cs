using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using be_nexus_fs.Controllers;
using be_nexus_fs.DTOs;
using be_nexus_fs.UseCases;

namespace NexusFS.Tests
{
    public class UsersControllerTests
    {
        [Fact]
        public async Task GetUserById_ReturnsExpectedUser()
        {
            var mockUseCase = new Mock<IGetUserByIdUseCase>();
            var expectedUser = new UserDto { /* set properties as needed */ };
            mockUseCase.Setup(u => u.ExecuteAsync(It.IsAny<string>())).ReturnsAsync(expectedUser);
            var controller = new UsersController(mockUseCase.Object /*, other dependencies */);
            var userId = "test-id";

            var result = await controller.GetUserById(userId);

            var okResult = Assert.IsType<ActionResult<UserDto>>(result);
            Assert.NotNull(okResult.Value);
            // Add more assertions as needed
        }
    }
}
