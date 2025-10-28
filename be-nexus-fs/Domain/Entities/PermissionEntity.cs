using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Entity representing a user permission in the database.
    /// </summary>
    public class PermissionEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Username { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Permission { get; set; }

        public DateTime GrantedAt { get; set; }
    }
}