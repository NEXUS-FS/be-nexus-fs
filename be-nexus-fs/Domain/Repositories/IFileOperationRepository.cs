namespace Domain.Repositories
{
    /// <summary>
    /// Repository interface for file operations on storage providers.
    /// Abstracts provider management from the Application layer.
    /// </summary>
    public interface IFileOperationRepository
    {
        /// <summary>
        /// Reads a file from the specified provider.
        /// </summary>
        Task<string> ReadFileAsync(string providerId, string filePath);

        /// <summary>
        /// Writes content to a file on the specified provider.
        /// </summary>
        Task WriteFileAsync(string providerId, string filePath, string content);

        /// <summary>
        /// Deletes a file from the specified provider.
        /// </summary>
        Task DeleteFileAsync(string providerId, string filePath);

        /// <summary>
        /// Lists files in a directory on the specified provider.
        /// </summary>
        Task<List<string>> ListFilesAsync(string providerId, string directoryPath, bool recursive);

        /// <summary>
        /// Checks if a provider exists and is active.
        /// </summary>
        Task<bool> ProviderExistsAsync(string providerId);
    }
}
