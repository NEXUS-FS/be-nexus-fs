using Application.DTOs.FileOperations;

namespace Application.UseCases.FileOperations.Commands
{
    /// <summary>
    /// Command to write content to a file on a storage provider.
    /// </summary>
    public class WriteFileCommand
    {
        public WriteFileRequest Request { get; set; } = null!;
    }

    /// <summary>
    /// Response for write file command.
    /// </summary>
    public class WriteFileCommandResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
