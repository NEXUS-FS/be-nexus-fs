using System;

namespace Domain.Models
{
    /// <summary>
    /// Represents metadata information for a file or directory.
    /// </summary>
    public class FileMetadata
    {
        /// <summary>
        /// Name of the file or directory
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Full path of the file or directory
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Size in bytes (0 for directories)
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Creation timestamp (UTC)
        /// </summary>
        public DateTime? Created { get; set; }

        /// <summary>
        /// Last modification timestamp (UTC)
        /// </summary>
        public DateTime? Modified { get; set; }

        /// <summary>
        /// Content type / MIME type (if available)
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Indicates if this is a directory
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Indicates if the file/directory exists
        /// </summary>
        public bool Exists { get; set; }

        /// <summary>
        /// Provider-specific metadata (optional)
        /// </summary>
        public Dictionary<string, string>? AdditionalMetadata { get; set; }
    }
}

