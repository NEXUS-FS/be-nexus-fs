using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{


    public class ChangeRoleRequest
    {
        [Required]
        public string NewRole { get; set; } = string.Empty;
    }
}