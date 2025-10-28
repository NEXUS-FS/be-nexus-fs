using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class AuditLogEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    public required string Action { get; set; }

    [MaxLength(500)]
    public string? ResourcePath { get; set; }

    public string? UserId { get; set; }

    public string? Details { get; set; } // JSON

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}