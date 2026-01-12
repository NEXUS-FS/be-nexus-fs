namespace Application.DTOs.FileOperations
{
    /// <summary>
    /// Request to move/rename a file.
    /// </summary>
    public class MoveFileRequest
    {
        /// <summary>
        /// Provider ID
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// Source file path
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Destination file path
        /// </summary>
        public string DestinationPath { get; set; } = string.Empty;

        /// <summary>
        /// User ID performing the operation
        /// </summary>
        public string UserId { get; set; } = string.Empty;
    }
}

