using Application.DTOs.FileOperations;

namespace Application.UseCases.FileOperations.Commands
{
    /// <summary>
    /// Command to delete a file from a storage provider.
    /// </summary>
    public class DeleteFileCommand
    {
        public DeleteFileRequest Request { get; set; } = null!;
    }

    /// <summary>
    /// Response for delete file command.
    /// </summary>
    public class DeleteFileCommandResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
