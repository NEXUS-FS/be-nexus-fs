namespace Application.DTOs.FileOperations
{
    /// <summary>
    /// Response DTO for listing files in a directory.
    /// </summary>
    public class ListFilesResponse
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Optional message providing additional context.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// List of file paths found in the directory.
        /// </summary>
        public List<string>? Files { get; set; }

        /// <summary>
        /// The directory that was listed.
        /// </summary>
        public string? DirectoryPath { get; set; }

        /// <summary>
        /// Timestamp of when the operation was performed.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
