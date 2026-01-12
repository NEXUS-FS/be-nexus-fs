using System;

namespace Application.DTOs.FileOperations
{
    /// <summary>
    /// Response for move operation.
    /// </summary>
    public class MoveFileResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? SourcePath { get; set; }
        public string? DestinationPath { get; set; }
    }
}

