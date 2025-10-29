using Domain.Entities;
using Domain.Repositories;
using Application.DTOs.User;
using Application.UseCases.Users.Commands;

namespace Application.UseCases.Users.CommandsHandler
{
    public class CreateUserHandler
    {
        private readonly IUserRepository _userRepository;

        public CreateUserHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto> HandleAsync(CreateUserCommand command)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(command.CreateUserDto.Username))
                throw new ArgumentException("Username is required");

            if (string.IsNullOrWhiteSpace(command.CreateUserDto.Email))
                throw new ArgumentException("Email is required");

            if (await _userRepository.UsernameExistsAsync(command.CreateUserDto.Username))
                throw new InvalidOperationException($"Username '{command.CreateUserDto.Username}' already exists");

            if (await _userRepository.EmailExistsAsync(command.CreateUserDto.Email))
                throw new InvalidOperationException($"Email '{command.CreateUserDto.Email}' already exists");

            // Create entity
            var userEntity = new UserEntity
            {
                Id = string.Empty, // Will be set by repository
                Username = command.CreateUserDto.Username,
                Email = command.CreateUserDto.Email,
                PasswordHash = command.CreateUserDto.Password, // Will be hashed by repository
                Provider = command.CreateUserDto.Provider,
                ProviderId = command.CreateUserDto.ProviderId,
                Role = command.CreateUserDto.Role
            };

            var createdUser = await _userRepository.AddAsync(userEntity);

            // Map to DTO
            return new UserDto
            {
                Id = createdUser.Id,
                Username = createdUser.Username,
                Email = createdUser.Email,
                Role = createdUser.Role,
                Provider = createdUser.Provider,
                IsActive = createdUser.IsActive,
                CreatedAt = createdUser.CreatedAt,
                UpdatedAt = createdUser.UpdatedAt,
                LastLogin = createdUser.LastLogin
            };
        }
    }
}