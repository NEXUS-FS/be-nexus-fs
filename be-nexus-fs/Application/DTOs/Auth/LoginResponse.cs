using System.ComponentModel.DataAnnotations;
using Application.DTOs.User;

namespace Application.DTOs.Auth
{

    // I got the job to put these AccessToken, RefreshToken, User info ;)

    //tokens are strings,  corectly?

    public class LoginResponse
    {

        [Required]
        public string AccessToken { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;


        [Required]
        public DateTime ExpiresAt { get; set; }
        //user info is the UserDto obj or smth else?
        [Required]
        public UserDto User { get; set; } = new UserDto();


    }
}