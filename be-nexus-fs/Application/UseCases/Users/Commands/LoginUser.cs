using Application.DTOs.Auth;
using Microsoft.OpenApi.Writers;

namespace Application.UseCases.Users.Commands
{
    public class LoginUserCommand
    {
        public LoginRequest LogRequest { get; set; } = new();
    }

    public class LoginUserResponse
    {
        public DateTime ExpiresAt { get; set; }
        public LoginResponse LogResopnse { get; set; } = new();


    }
}