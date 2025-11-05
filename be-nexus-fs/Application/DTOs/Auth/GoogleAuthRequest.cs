using System.ComponentModel.DataAnnotations;

namespace  Application.DTOs.Auth
{

    public class GoogleAuthRequest
    {

        //just the id?
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }
}