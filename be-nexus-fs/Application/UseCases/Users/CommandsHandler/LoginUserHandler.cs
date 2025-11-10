using Domain.Repositories;
using Application.DTOs.Auth;
using Application.UseCases.Users.Commands;
using Application.DTOs.User;

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
            var request = command.LogRequest;

            // Validate credentials (where BCrypt or hashing logic is used)
            var user = await _userRepository.ValidateCredentialsAsync(request.Username, request.Password);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid username or password.");

            // update last login timestamp
            await _userRepository.UpdateLastLoginAsync(user.Id);

            // //TODO THIS
            var accessToken = "TODO-GENERATE-JWT-TOKEN";
            var refreshToken = "TODO-GENERATE-REFRESH-TOKEN";

            var expiresAt = DateTime.UtcNow.AddHours(24);

            // build the response DTO
            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role
                }
            };

            return new LoginUserResponse
            {
                ExpiresAt = expiresAt,
                LogResopnse = response
            };
        }
    }
}
