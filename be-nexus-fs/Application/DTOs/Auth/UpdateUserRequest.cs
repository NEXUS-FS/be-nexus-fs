
//- Email, Username (optional password change)

using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{

    //we use ? because those can be NULLABLE , we can update just a field or none... but not really everything

    //but my task says only password to be optional nullable... so I will follow that i guess?

    //we may want maybe the user id here too?  because username can be misleading?
    public class UpdateUserRequest
    {

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;


        //this is optional
        [StringLength(100, MinimumLength = 3)]
        public string? Password { get; set; }


    }
}