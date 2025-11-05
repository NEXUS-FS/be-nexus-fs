using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{

    public class RegisterRequest
    {

        //username
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        //email
        [Required]
        [EmailAddress]// that is neat
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

}