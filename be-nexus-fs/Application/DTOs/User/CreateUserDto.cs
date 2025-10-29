namespace Application.DTOs.User
{
    public class CreateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string Provider { get; set; } = "Basic";
        public string? ProviderId { get; set; }
        public string Role { get; set; } = "User";
    }
}