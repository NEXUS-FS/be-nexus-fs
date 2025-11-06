using Domain.Repositories;
using Application.DTOs.User;

namespace Application.UseCases.Users.Queries
{
    public class GetUserByIdHandler
    {
        private readonly IUserRepository _userRepository;

        public GetUserByIdHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto?> HandleAsync(GetUserByIdQuery query)
        {
            var user = await _userRepository.GetByIdAsync(query.UserId);

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
