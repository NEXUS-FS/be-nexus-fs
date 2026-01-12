using System;

namespace Application.DTOs.FileOperations
{
    /// <summary>
    /// Response indicating if file/directory exists.
    /// </summary>
    public class ExistsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Path { get; set; }
        public bool Exists { get; set; }
    }
}

