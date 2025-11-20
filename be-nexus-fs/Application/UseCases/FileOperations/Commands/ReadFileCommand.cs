using Application.DTOs.FileOperations;

namespace Application.UseCases.FileOperations.Commands
{
    /// <summary>
    /// Command to read a file from a storage provider.
    /// </summary>
    public class ReadFileCommand
    {
        public ReadFileRequest Request { get; set; } = null!;
    }

    /// <summary>
    /// Response for read file command.
    /// </summary>
    public class ReadFileCommandResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
