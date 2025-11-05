using System.ComponentModel.DataAnnotations; //we can do some automation on checking with if (!ModelState.IsValid){} at runtime easier

//so we need some validation attributes..

//to log we need some credidentials (obv yes!)

//most used name-password so these are required.

//we can add more metadata but for login this may be enough now?

namespace Application.DTOs.Auth
{
    public class LoginRequest //lets prefix this with DTO? LoginRequestDTO?..
    {
        [Required]
        [StringLength(100, MinimumLength = 3)] //for ex max 100,min 3, we can toy arond
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Password { get; set; } = string.Empty;

        //more required fields..

    }
}