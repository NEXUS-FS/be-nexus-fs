using Domain.Repositories;
using Application.DTOs.User;

namespace Application.UseCases.Users.Queries
{
    public class GetUserByEmailHandler
    {
        private readonly IUserRepository _userRepository;

        public GetUserByEmailHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto?> HandleAsync(GetUserByEmailQuery query)
        {
            var user = await _userRepository.GetByEmailAsync(query.Email);

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