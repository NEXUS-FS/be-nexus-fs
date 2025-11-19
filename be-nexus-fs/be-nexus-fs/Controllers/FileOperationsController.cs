using Microsoft.AspNetCore.Mvc;
using Application.DTOs.FileOperations;
using Application.UseCases.FileOperations.Commands;
using Application.UseCases.FileOperations.CommandsHandler;

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
        private readonly ReadFileHandler _readFileHandler;
        private readonly WriteFileHandler _writeFileHandler;
        private readonly DeleteFileHandler _deleteFileHandler;
        private readonly ListFilesHandler _listFilesHandler;
        private readonly ILogger<FileOperationsController> _logger;

        public FileOperationsController(
            ReadFileHandler readFileHandler,
            WriteFileHandler writeFileHandler,
            DeleteFileHandler deleteFileHandler,
            ListFilesHandler listFilesHandler,
            ILogger<FileOperationsController> logger)
        {
            _readFileHandler = readFileHandler;
            _writeFileHandler = writeFileHandler;
            _deleteFileHandler = deleteFileHandler;
            _listFilesHandler = listFilesHandler;
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
        [ProducesResponseType(typeof(ReadFileCommandResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ReadFileCommandResponse>> ReadFile([FromBody] ReadFileRequest request)
        {
            try
            {
                var command = new ReadFileCommand { Request = request };
                var result = await _readFileHandler.HandleAsync(command);

                if (!result.Success)
                {
                    if (result.Message?.Contains("not found") == true)
                    {
                        return NotFound(result);
                    }
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReadFile endpoint");
                return StatusCode(500, new ReadFileCommandResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
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
        [ProducesResponseType(typeof(WriteFileCommandResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<WriteFileCommandResponse>> WriteFile([FromBody] WriteFileRequest request)
        {
            try
            {
                var command = new WriteFileCommand { Request = request };
                var result = await _writeFileHandler.HandleAsync(command);

                if (!result.Success)
                {
                    if (result.Message?.Contains("not found") == true)
                    {
                        return NotFound(result);
                    }
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WriteFile endpoint");
                return StatusCode(500, new WriteFileCommandResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
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
        [ProducesResponseType(typeof(DeleteFileCommandResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DeleteFileCommandResponse>> DeleteFile([FromBody] DeleteFileRequest request)
        {
            try
            {
                var command = new DeleteFileCommand { Request = request };
                var result = await _deleteFileHandler.HandleAsync(command);

                if (!result.Success)
                {
                    if (result.Message?.Contains("not found") == true)
                    {
                        return NotFound(result);
                    }
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteFile endpoint");
                return StatusCode(500, new DeleteFileCommandResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
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
        [ProducesResponseType(typeof(ListFilesCommandResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ListFilesCommandResponse>> ListFiles(
            [FromQuery] string providerId,
            [FromQuery] string directoryPath,
            [FromQuery] bool recursive = false,
            [FromQuery] string? userId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(providerId))
                {
                    return BadRequest(new ListFilesCommandResponse
                    {
                        Success = false,
                        Message = "ProviderId is required",
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    return BadRequest(new ListFilesCommandResponse
                    {
                        Success = false,
                        Message = "DirectoryPath is required",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var request = new ListFilesRequest
                {
                    ProviderId = providerId,
                    DirectoryPath = directoryPath,
                    Recursive = recursive,
                    UserId = userId ?? string.Empty
                };

                var command = new ListFilesCommand { Request = request };
                var result = await _listFilesHandler.HandleAsync(command);

                if (!result.Success)
                {
                    if (result.Message?.Contains("not found") == true)
                    {
                        return NotFound(result);
                    }
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ListFiles endpoint");
                return StatusCode(500, new ListFilesCommandResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
