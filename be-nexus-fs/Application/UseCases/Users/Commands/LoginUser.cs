using Application.DTOs.Auth;

namespace Application.UseCases.Users.Commands
{
    public class LoginUserCommand
    {
       public LoginRequest loginRequest { get; set; } = new();
    }

    public class LoginUserResponse
    {
        public LoginResponse loginResponse { get; set; } = new();
    }
}