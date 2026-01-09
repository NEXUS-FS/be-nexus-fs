namespace Application.DTOs.FileOperations
{
    /// <summary>
    /// Request to create a directory.
    /// </summary>
    public class MkdirRequest
    {
        /// <summary>
        /// Provider ID
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// Directory path to create
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Create parent directories if they don't exist
        /// </summary>
        public bool Recursive { get; set; } = true;

        /// <summary>
        /// User ID performing the operation
        /// </summary>
        public string UserId { get; set; } = string.Empty;
    }
}

