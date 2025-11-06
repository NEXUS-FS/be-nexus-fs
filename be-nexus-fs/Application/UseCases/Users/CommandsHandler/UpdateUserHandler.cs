using Domain.Repositories;
using Application.UseCases.Users.Commands;

namespace Application.UseCases.Users.CommandsHandler
{
    public class UpdateUserHandler
    {
        private readonly IUserRepository _userRepository;

        public UpdateUserHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task HandleAsync(UpdateUserCommand command)
        {
            var existingUser = await _userRepository.GetByIdAsync(command.UserId);

            if (existingUser == null)
                throw new KeyNotFoundException($"User with ID '{command.UserId}' not found");

            if (command.UpdateUserDto.Username != existingUser.Username &&
                await _userRepository.UsernameExistsAsync(command.UpdateUserDto.Username, command.UserId))
            {
                throw new InvalidOperationException($"Username '{command.UpdateUserDto.Username}' already exists");
            }

            if (command.UpdateUserDto.Email != existingUser.Email &&
                await _userRepository.EmailExistsAsync(command.UpdateUserDto.Email, command.UserId))
            {
                throw new InvalidOperationException($"Email '{command.UpdateUserDto.Email}' already exists");
            }

            existingUser.Username = command.UpdateUserDto.Username;
            existingUser.Email = command.UpdateUserDto.Email;
            existingUser.Role = command.UpdateUserDto.Role;
            existingUser.IsActive = command.UpdateUserDto.IsActive;

            if (!string.IsNullOrWhiteSpace(command.UpdateUserDto.NewPassword))
            {
                existingUser.PasswordHash = command.UpdateUserDto.NewPassword;
            }

            await _userRepository.UpdateAsync(existingUser);
        }
    }
}