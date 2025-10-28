using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Entity representing a storage provider (e.g., AWS S3, Azure Blob, Local FileSystem).
    /// </summary>
    public class ProviderEntity
    {
        [Key]
        [MaxLength(100)]
        public required string Id { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Type { get; set; } // e.g., "S3", "Azure", "Local"

        public bool IsActive { get; set; } = true;

        public string? Configuration { get; set; } // JSON configuration

        public int Priority { get; set; } = 0; // Lower value = higher priority

        public long? MaxFileSize { get; set; } // Max file size in bytes

        public string? SupportedFileTypes { get; set; } // Comma-separated extensions

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; } // For soft delete
    }
}