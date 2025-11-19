namespace Application.DTOs.FileOperations
{
    /// <summary>
    /// Request DTO for listing files in a directory.
    /// </summary>
    public class ListFilesRequest
    {
        /// <summary>
        /// The unique identifier of the storage provider.
        /// </summary>
        public required string ProviderId { get; set; }

        /// <summary>
        /// The directory path to list files from.
        /// </summary>
        public required string DirectoryPath { get; set; }

        /// <summary>
        /// Whether to list files recursively in subdirectories.
        /// </summary>
        public bool Recursive { get; set; } = false;

        /// <summary>
        /// The user identifier performing the operation.
        /// </summary>
        public required string UserId { get; set; }
    }
}
