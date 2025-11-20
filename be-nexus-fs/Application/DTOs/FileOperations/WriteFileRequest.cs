namespace Application.DTOs.FileOperations
{
    /// <summary>
    /// Request DTO for writing file content.
    /// </summary>
    public class WriteFileRequest
    {
        /// <summary>
        /// The unique identifier of the storage provider.
        /// </summary>
        public required string ProviderId { get; set; }

        /// <summary>
        /// The path where the file should be written.
        /// </summary>
        public required string FilePath { get; set; }

        /// <summary>
        /// The content to write to the file.
        /// </summary>
        public required string Content { get; set; }

        /// <summary>
        /// The user identifier performing the operation.
        /// </summary>
        public required string UserId { get; set; }
    }
}
