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

    public async Task<LoginUserResponse> HandleAsync(LoginUserCommand command)
    {
        var loginDto = command.loginRequest;

        var user = await _userRepository.ValidateCredentialsAsync(
            loginDto.Username,
            loginDto.Password
        );

        if (user == null)
            throw new UnauthorizedAccessException("Invalid username or password.");

        await _userRepository.UpdateLastLoginAsync(user.Id);

        LoginUserResponse lg= new LoginUserResponse();
        

        var loginResponse =lg.loginResponse;
        {
            var AccessToken = _jwtTokenService.GenerateAccessToken(user);
            var RefreshToken = _jwtTokenService.GenerateRefreshToken();

            var User = new Application.DTOs.User.UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };
            loginResponse.AccessToken = AccessToken;
            loginResponse.RefreshToken = RefreshToken;
            loginResponse.User = User;

        };

        return new LoginUserResponse
        {
            loginResponse = loginResponse
        };
    }
}
