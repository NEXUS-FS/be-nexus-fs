using Domain.Repositories;
using Application.DTOs.User;

namespace Application.UseCases.Users.Queries
{
    public class GetUserByUsernameHandler
    {
        private readonly IUserRepository _userRepository;

        public GetUserByUsernameHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto?> HandleAsync(GetUserByUsernameQuery query)
        {
            var user = await _userRepository.GetByUsernameAsync(query.Username);

            if (user == null)
                return null;

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                Provider = user.Provider,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLogin = user.LastLogin
            };
        }
    }
}