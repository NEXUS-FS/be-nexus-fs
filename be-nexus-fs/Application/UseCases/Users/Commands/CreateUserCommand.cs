using Application.DTOs.User;

namespace Application.UseCases.Users.Commands
{
    public class CreateUserCommand
    {
        public CreateUserDto CreateUserDto { get; set; } = new();
    }
}