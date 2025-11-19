using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.DTOs.FileOperations;
using Domain.Repositories;
using Infrastructure.Services;
using System.Text.Json;

namespace be_nexus_fs.Controllers
{
    /// <summary>
    /// Controller for file operations across different storage providers.
    /// </summary>
    [ApiController]
    [Route("api/files")]
    [Produces("application/json")]
    public class FileOperationsController : ControllerBase
    {
        private readonly IProviderRepository _providerRepository;
        private readonly ProviderFactory _providerFactory;
        private readonly ILogger<FileOperationsController> _logger;

        public FileOperationsController(
            IProviderRepository providerRepository,
            ProviderFactory providerFactory,
            ILogger<FileOperationsController> logger)
        {
            _providerRepository = providerRepository;
            _providerFactory = providerFactory;
            _logger = logger;
        }

        /// <summary>
        /// Read file content from a storage provider.
        /// </summary>
        /// <param name="request">Read file request containing provider ID, file path, and user ID</param>
        /// <returns>File content and operation status</returns>
        /// <response code="200">File read successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="404">Provider or file not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("read")]
        [ProducesResponseType(typeof(FileOperationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FileOperationResponse>> ReadFile([FromBody] ReadFileRequest request)
        {
            try
            {
                _logger.LogInformation("Reading file {FilePath} from provider {ProviderId} for user {UserId}",
                    request.FilePath, request.ProviderId, request.UserId);

                var provider = await GetProviderInstanceAsync(request.ProviderId);
                if (provider == null)
                {
                    return NotFound(new FileOperationResponse
                    {
                        Success = false,
                        Message = $"Provider '{request.ProviderId}' not found",
                        Timestamp = DateTime.UtcNow,
                        Operation = Application.Common.FileOperation.Read
                    });
                }

                var content = await provider.ReadFileAsync(request.FilePath);

                return Ok(new FileOperationResponse
                {
                    Success = true,
                    Message = "File read successfully",
                    Content = content,
                    Timestamp = DateTime.UtcNow,
                    Operation = Application.Common.FileOperation.Read
                });
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "File not found: {FilePath}", request.FilePath);
                return NotFound(new FileOperationResponse
                {
                    Success = false,
                    Message = $"File not found: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Operation = Application.Common.FileOperation.Read
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file {FilePath} from provider {ProviderId}",
                    request.FilePath, request.ProviderId);
                return StatusCode(500, new FileOperationResponse
                {
                    Success = false,
                    Message = $"Error reading file: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Operation = Application.Common.FileOperation.Read
                });
            }
        }

        /// <summary>
        /// Write content to a file on a storage provider.
        /// </summary>
        /// <param name="request">Write file request containing provider ID, file path, content, and user ID</param>
        /// <returns>Operation status</returns>
        /// <response code="200">File written successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="404">Provider not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("write")]
        [ProducesResponseType(typeof(FileOperationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FileOperationResponse>> WriteFile([FromBody] WriteFileRequest request)
        {
            try
            {
                _logger.LogInformation("Writing file {FilePath} to provider {ProviderId} for user {UserId}",
                    request.FilePath, request.ProviderId, request.UserId);

                var provider = await GetProviderInstanceAsync(request.ProviderId);
                if (provider == null)
                {
                    return NotFound(new FileOperationResponse
                    {
                        Success = false,
                        Message = $"Provider '{request.ProviderId}' not found",
                        Timestamp = DateTime.UtcNow,
                        Operation = Application.Common.FileOperation.Write
                    });
                }

                await provider.WriteFileAsync(request.FilePath, request.Content);

                return Ok(new FileOperationResponse
                {
                    Success = true,
                    Message = "File written successfully",
                    Timestamp = DateTime.UtcNow,
                    Operation = Application.Common.FileOperation.Write
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file {FilePath} to provider {ProviderId}",
                    request.FilePath, request.ProviderId);
                return StatusCode(500, new FileOperationResponse
                {
                    Success = false,
                    Message = $"Error writing file: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Operation = Application.Common.FileOperation.Write
                });
            }
        }

        /// <summary>
        /// Delete a file from a storage provider.
        /// </summary>
        /// <param name="request">Delete file request containing provider ID, file path, and user ID</param>
        /// <returns>Operation status</returns>
        /// <response code="200">File deleted successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="404">Provider or file not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete]
        [ProducesResponseType(typeof(FileOperationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FileOperationResponse>> DeleteFile([FromBody] DeleteFileRequest request)
        {
            try
            {
                _logger.LogInformation("Deleting file {FilePath} from provider {ProviderId} for user {UserId}",
                    request.FilePath, request.ProviderId, request.UserId);

                var provider = await GetProviderInstanceAsync(request.ProviderId);
                if (provider == null)
                {
                    return NotFound(new FileOperationResponse
                    {
                        Success = false,
                        Message = $"Provider '{request.ProviderId}' not found",
                        Timestamp = DateTime.UtcNow,
                        Operation = Application.Common.FileOperation.Delete
                    });
                }

                await provider.DeleteFileAsync(request.FilePath);

                return Ok(new FileOperationResponse
                {
                    Success = true,
                    Message = "File deleted successfully",
                    Timestamp = DateTime.UtcNow,
                    Operation = Application.Common.FileOperation.Delete
                });
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "File not found: {FilePath}", request.FilePath);
                return NotFound(new FileOperationResponse
                {
                    Success = false,
                    Message = $"File not found: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Operation = Application.Common.FileOperation.Delete
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FilePath} from provider {ProviderId}",
                    request.FilePath, request.ProviderId);
                return StatusCode(500, new FileOperationResponse
                {
                    Success = false,
                    Message = $"Error deleting file: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Operation = Application.Common.FileOperation.Delete
                });
            }
        }

        /// <summary>
        /// List files in a directory on a storage provider.
        /// </summary>
        /// <param name="providerId">Provider ID</param>
        /// <param name="directoryPath">Directory path to list files from</param>
        /// <param name="recursive">Whether to list files recursively</param>
        /// <param name="userId">User ID performing the operation</param>
        /// <returns>List of files in the directory</returns>
        /// <response code="200">Files listed successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="404">Provider or directory not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("list")]
        [ProducesResponseType(typeof(ListFilesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ListFilesResponse>> ListFiles(
            [FromQuery] string providerId,
            [FromQuery] string directoryPath,
            [FromQuery] bool recursive = false,
            [FromQuery] string? userId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(providerId))
                {
                    return BadRequest(new ListFilesResponse
                    {
                        Success = false,
                        Message = "ProviderId is required",
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    return BadRequest(new ListFilesResponse
                    {
                        Success = false,
                        Message = "DirectoryPath is required",
                        Timestamp = DateTime.UtcNow
                    });
                }

                _logger.LogInformation("Listing files in {DirectoryPath} from provider {ProviderId} for user {UserId}",
                    directoryPath, providerId, userId);

                var provider = await GetProviderInstanceAsync(providerId);
                if (provider == null)
                {
                    return NotFound(new ListFilesResponse
                    {
                        Success = false,
                        Message = $"Provider '{providerId}' not found",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var files = await provider.ListFilesAsync(directoryPath, recursive);

                return Ok(new ListFilesResponse
                {
                    Success = true,
                    Message = "Files listed successfully",
                    Files = files,
                    DirectoryPath = directoryPath,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogWarning(ex, "Directory not found: {DirectoryPath}", directoryPath);
                return NotFound(new ListFilesResponse
                {
                    Success = false,
                    Message = $"Directory not found: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files in {DirectoryPath} from provider {ProviderId}",
                    directoryPath, providerId);
                return StatusCode(500, new ListFilesResponse
                {
                    Success = false,
                    Message = $"Error listing files: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Helper method to get a provider instance from the repository.
        /// </summary>
        private async Task<Provider?> GetProviderInstanceAsync(string providerId)
        {
            var providerEntity = await _providerRepository.GetByIdAsync(providerId);
            if (providerEntity == null || !providerEntity.IsActive)
            {
                return null;
            }

            var configuration = string.IsNullOrWhiteSpace(providerEntity.Configuration)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(providerEntity.Configuration)
                  ?? new Dictionary<string, string>();

            var provider = await _providerFactory.CreateProviderAsync(
                providerEntity.Type,
                providerEntity.Id,
                configuration);

            return provider;
        }
    }
}
