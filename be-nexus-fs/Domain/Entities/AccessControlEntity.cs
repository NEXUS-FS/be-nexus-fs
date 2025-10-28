using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class AccessControlEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public required string UserId { get; set; }

    [Required]
    [MaxLength(500)]
    public required string ResourcePath { get; set; }

    public string? Permissions { get; set; } // JSON or comma-separated

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}