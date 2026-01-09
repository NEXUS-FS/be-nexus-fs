namespace Application.DTOs.FileOperations
{
    /// <summary>
    /// Request to get file/directory metadata (stat operation).
    /// </summary>
    public class StatFileRequest
    {
        /// <summary>
        /// Provider ID
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// File or directory path
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// User ID performing the operation
        /// </summary>
        public string UserId { get; set; } = string.Empty;
    }
}

