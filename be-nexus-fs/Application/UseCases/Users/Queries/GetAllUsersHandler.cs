using Domain.Repositories;
using Application.DTOs.User;
using Application.DTOs.Common;

namespace Application.UseCases.Users.Queries
{
    public class GetAllUsersHandler
    {
        private readonly IUserRepository _userRepository;

        public GetAllUsersHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<PagedResponse<UserDto>> HandleAsync(GetAllUsersQuery query)
        {
            var users = await _userRepository.GetAllAsync(query.PageNumber, query.PageSize);
            var totalCount = await _userRepository.GetTotalCountAsync();

            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                Provider = u.Provider,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                LastLogin = u.LastLogin
            });

            return new PagedResponse<UserDto>
            {
                Data = userDtos,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }
    }
}