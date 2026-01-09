using System;
using Domain.Models;

namespace Application.DTOs.FileOperations
{
    /// <summary>
    /// Response containing file/directory metadata.
    /// </summary>
    public class StatFileResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public FileMetadata? Metadata { get; set; }
    }
}

