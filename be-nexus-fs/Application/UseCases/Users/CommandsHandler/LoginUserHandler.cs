using Application.UseCases.Users.Commands;
using Application.DTOs.Auth;
using Domain.Repositories;
using Application.Common.Security;

public class LoginUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginUserHandler(IUserRepository userRepository, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> HandleAsync(LoginUserCommand command)
    {
        var loginDto = command.loginRequest;

        var user = await _userRepository.ValidateCredentialsAsync(
            loginDto.Username,
            loginDto.Password
        );

        if (user == null)
            throw new UnauthorizedAccessException("Invalid username or password.");

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        await _userRepository.UpdateLastLoginAsync(user.Id);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60), 
            User = new Application.DTOs.User.UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            }
        };
    }
}