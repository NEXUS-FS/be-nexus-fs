using Domain.Repositories;
using Application.UseCases.Users.Commands;

namespace Application.UseCases.Users.CommandsHandler
{
    public class DeleteUserHandler
    {
        private readonly IUserRepository _userRepository;

        public DeleteUserHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task HandleAsync(DeleteUserCommand command)
        {
            await _userRepository.DeleteAsync(command.UserId);
        }
    }
};

