namespace Application.DTOs.FileOperations
{
    /// <summary>
    /// Request DTO for deleting a file.
    /// </summary>
    public class DeleteFileRequest
    {
        /// <summary>
        /// The unique identifier of the storage provider.
        /// </summary>
        public required string ProviderId { get; set; }

        /// <summary>
        /// The path to the file to be deleted.
        /// </summary>
        public required string FilePath { get; set; }

        /// <summary>
        /// The user identifier performing the operation.
        /// </summary>
        public required string UserId { get; set; }
    }
}
