using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

/// <summary>
/// Entity representing a system metric.
/// </summary>
public class MetricEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    public required string MetricName { get; set; }

    public double Value { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    [MaxLength(100)]
    public string? ProviderId { get; set; }

    [MaxLength(50)]
    public string? ProviderType { get; set; }

    public string? Tags { get; set; } // JSON string

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}