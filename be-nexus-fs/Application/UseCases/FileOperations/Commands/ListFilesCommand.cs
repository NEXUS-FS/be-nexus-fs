using Application.DTOs.FileOperations;

namespace Application.UseCases.FileOperations.Commands
{
    /// <summary>
    /// Command to list files in a directory on a storage provider.
    /// </summary>
    public class ListFilesCommand
    {
        public ListFilesRequest Request { get; set; } = null!;
    }

    /// <summary>
    /// Response for list files command.
    /// </summary>
    public class ListFilesCommandResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string>? Files { get; set; }
        public string? DirectoryPath { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
