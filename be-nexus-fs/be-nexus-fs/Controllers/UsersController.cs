using Microsoft.AspNetCore.Mvc;
using Application.DTOs.User;
using Application.DTOs.Common;
using Application.UseCases.Users.CommandsHandler;
using Application.UseCases.Users.Commands;
using Application.UseCases.Users.Queries;

namespace be_nexus_fs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly CreateUserHandler _createUserHandler;
        private readonly UpdateUserHandler _updateUserHandler;
        private readonly DeleteUserHandler _deleteUserHandler;
        private readonly LoginUserHandler _loginUserHandler;
        private readonly GetUserByIdHandler _getUserByIdHandler;
        private readonly GetAllUsersHandler _getAllUsersHandler;
        private readonly GetUserByUsernameHandler _getUserByUsernameHandler;
        private readonly GetUserByEmailHandler _getUserByEmailHandler;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            CreateUserHandler createUserHandler,
            UpdateUserHandler updateUserHandler,
            DeleteUserHandler deleteUserHandler,
            LoginUserHandler loginUserHandler,
            GetUserByIdHandler getUserByIdHandler,
            GetAllUsersHandler getAllUsersHandler,
            GetUserByUsernameHandler getUserByUsernameHandler,
            GetUserByEmailHandler getUserByEmailHandler,
            ILogger<UsersController> logger)
        {
            _createUserHandler = createUserHandler;
            _updateUserHandler = updateUserHandler;
            _deleteUserHandler = deleteUserHandler;
            _loginUserHandler = loginUserHandler;
            _getUserByIdHandler = getUserByIdHandler;
            _getAllUsersHandler = getAllUsersHandler;
            _getUserByUsernameHandler = getUserByUsernameHandler;
            _getUserByEmailHandler = getUserByEmailHandler;
            _logger = logger;
        }

        /// <summary>
        /// Get all users with pagination.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<UserDto>>> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = new GetAllUsersQuery
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var result = await _getAllUsersHandler.HandleAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, "An error occurred while retrieving users");
            }
        }

        /// <summary>
        /// Get a specific user by ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUserById(string id)
        {
            try
            {
                var query = new GetUserByIdQuery { UserId = id };
                var result = await _getUserByIdHandler.HandleAsync(query);

                if (result == null)
                    return NotFound($"User with ID '{id}' not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }

        /// <summary>
        /// Create a new user.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto dto)
        {
            try
            {
                var command = new CreateUserCommand { CreateUserDto = dto };  // FIXED: Changed from User to CreateUserDto
                var result = await _createUserHandler.HandleAsync(command);

                return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "An error occurred while creating the user");
            }
        }

        /// <summary>
        /// Update an existing user.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var command = new UpdateUserCommand { UserId = id, UpdateUserDto = dto };  // FIXED: Changed from User to UpdateUserDto
                await _updateUserHandler.HandleAsync(command);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, "An error occurred while updating the user");
            }
        }

        /// <summary>
        /// Delete a user (soft delete).
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var command = new DeleteUserCommand { UserId = id };
                await _deleteUserHandler.HandleAsync(command);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, "An error occurred while deleting the user");
            }
        }

        /// <summary>
        /// Authenticate a user.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginUserResponse>> Login([FromBody] LoginUserCommand command)
        {
            try
            {
                var result = await _loginUserHandler.HandleAsync(command);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, "An error occurred during authentication");
            }
        }

        /// <summary>
        /// Get user by username.
        /// </summary>
        [HttpGet("by-username/{username}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetByUsername(string username)
        {
            try
            {
                var query = new GetUserByUsernameQuery { Username = username };
                var result = await _getUserByUsernameHandler.HandleAsync(query);

                if (result == null)
                    return NotFound($"User '{username}' not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username {Username}", username);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }

        /// <summary>
        /// Get user by email.
        /// </summary>
        [HttpGet("by-email/{email}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetByEmail(string email)
        {
            try
            {
                var query = new GetUserByEmailQuery { Email = email };
                var result = await _getUserByEmailHandler.HandleAsync(query);

                if (result == null)
                    return NotFound($"User with email '{email}' not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email {Email}", email);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }
    }
}
