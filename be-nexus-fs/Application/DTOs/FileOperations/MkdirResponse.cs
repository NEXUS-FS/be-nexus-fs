using System;

namespace Application.DTOs.FileOperations
{
    /// <summary>
    /// Response for mkdir operation.
    /// </summary>
    public class MkdirResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Path { get; set; }
    }
}

