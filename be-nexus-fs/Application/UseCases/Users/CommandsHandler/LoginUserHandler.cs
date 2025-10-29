using Domain.Repositories;
using Application.UseCases.Users.Commands;

namespace Application.UseCases.Users.CommandsHandler
{
    public class LoginUserHandler
    {
        private readonly IUserRepository _userRepository;

        public LoginUserHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<LoginUserResponse> HandleAsync(LoginUserCommand command)
        {
            // Validate credentials in repository (where BCrypt is)
            var user = await _userRepository.ValidateCredentialsAsync(command.Username, command.Password);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid username or password");

            // Update last login
            await _userRepository.UpdateLastLoginAsync(user.Id);

            return new LoginUserResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                Token = "TODO-GENERATE-JWT-TOKEN",
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }
    }
}