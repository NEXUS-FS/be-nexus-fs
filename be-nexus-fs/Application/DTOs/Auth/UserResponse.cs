

//because this is a response we do not need validations I guess as we know we do things right?

// Id, Username, Email, Role, CreatedAt, IsActive (no password)

namespace Application.DTOs.Auth
{
    public class UserResponse
    {

        public string Id { get; set; } = string.Empty; //conform UserDto this is a string, this can be a Guid as well maybe to sure be UNIQUE??

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public Boolean IsActive { get; set; }
    }
}