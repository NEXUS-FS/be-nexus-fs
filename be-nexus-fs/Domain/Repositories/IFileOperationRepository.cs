using Domain.Models;

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
        /// Gets metadata for a file or directory.
        /// </summary>
        Task<FileMetadata> StatAsync(string providerId, string path);

        /// <summary>
        /// Creates a directory on the specified provider.
        /// </summary>
        Task MkdirAsync(string providerId, string path, bool recursive);

        /// <summary>
        /// Copies a file on the specified provider.
        /// </summary>
        Task CopyAsync(string providerId, string sourcePath, string destinationPath);

        /// <summary>
        /// Moves a file on the specified provider.
        /// </summary>
        Task MoveAsync(string providerId, string sourcePath, string destinationPath);

        /// <summary>
        /// Checks if a file or directory exists on the specified provider.
        /// </summary>
        Task<bool> ExistsAsync(string providerId, string path);

        /// <summary>
        /// Reads a file as a stream for large file handling.
        /// </summary>
        Task<Stream> ReadStreamAsync(string providerId, string filePath);

        /// <summary>
        /// Writes a file from a stream for large file handling.
        /// </summary>
        Task WriteStreamAsync(string providerId, string filePath, Stream content);

        /// <summary>
        /// Checks if a provider exists and is active.
        /// </summary>
        Task<bool> ProviderExistsAsync(string providerId);
    }
}
