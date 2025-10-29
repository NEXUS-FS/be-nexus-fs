using Application.DTOs.User;

namespace Application.UseCases.Users.Commands
{
    public class UpdateUserCommand
    {
        public string UserId { get; set; } = string.Empty;
        public UpdateUserDto UpdateUserDto { get; set; } = new();
    }
};

