using Microsoft.AspNetCore.Mvc;
using Application.DTOs.FileOperations;
using Application.UseCases.FileOperations.Commands;
using Application.UseCases.FileOperations.CommandsHandler;
using Domain.Repositories;

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
        private readonly StatFileHandler _statFileHandler;
        private readonly MkdirHandler _mkdirHandler;
        private readonly CopyFileHandler _copyFileHandler;
        private readonly MoveFileHandler _moveFileHandler;
        private readonly ExistsHandler _existsHandler;
        private readonly IFileOperationRepository _fileOperationRepository;
        private readonly ILogger<FileOperationsController> _logger;

        public FileOperationsController(
            ReadFileHandler readFileHandler,
            WriteFileHandler writeFileHandler,
            DeleteFileHandler deleteFileHandler,
            ListFilesHandler listFilesHandler,
            StatFileHandler statFileHandler,
            MkdirHandler mkdirHandler,
            CopyFileHandler copyFileHandler,
            MoveFileHandler moveFileHandler,
            ExistsHandler existsHandler,
            IFileOperationRepository fileOperationRepository,
            ILogger<FileOperationsController> logger)
        {
            _readFileHandler = readFileHandler;
            _writeFileHandler = writeFileHandler;
            _deleteFileHandler = deleteFileHandler;
            _listFilesHandler = listFilesHandler;
            _statFileHandler = statFileHandler;
            _mkdirHandler = mkdirHandler;
            _copyFileHandler = copyFileHandler;
            _moveFileHandler = moveFileHandler;
            _existsHandler = existsHandler;
            _fileOperationRepository = fileOperationRepository;
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

        /// <summary>
        /// Get file/directory metadata.
        /// </summary>
        /// <param name="request">Stat request containing provider ID, path, and user ID</param>
        /// <returns>File metadata</returns>
        /// <response code="200">Metadata retrieved successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="404">Provider or file not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("stat")]
        [ProducesResponseType(typeof(StatFileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<StatFileResponse>> StatFile([FromBody] StatFileRequest request)
        {
            try
            {
                var command = new StatFileCommand { Request = request };
                var result = await _statFileHandler.HandleAsync(command);

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
                _logger.LogError(ex, "Error in StatFile endpoint");
                return StatusCode(500, new StatFileResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Create a directory.
        /// </summary>
        /// <param name="request">Mkdir request containing provider ID, path, recursive flag, and user ID</param>
        /// <returns>Operation status</returns>
        /// <response code="200">Directory created successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="404">Provider not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("mkdir")]
        [ProducesResponseType(typeof(MkdirResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MkdirResponse>> CreateDirectory([FromBody] MkdirRequest request)
        {
            try
            {
                var command = new MkdirCommand { Request = request };
                var result = await _mkdirHandler.HandleAsync(command);

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
                _logger.LogError(ex, "Error in Mkdir endpoint");
                return StatusCode(500, new MkdirResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Copy a file.
        /// </summary>
        /// <param name="request">Copy request containing provider ID, source path, destination path, and user ID</param>
        /// <returns>Operation status</returns>
        /// <response code="200">File copied successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="404">Provider or source file not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("copy")]
        [ProducesResponseType(typeof(CopyFileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CopyFileResponse>> CopyFile([FromBody] CopyFileRequest request)
        {
            try
            {
                var command = new CopyFileCommand { Request = request };
                var result = await _copyFileHandler.HandleAsync(command);

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
                _logger.LogError(ex, "Error in CopyFile endpoint");
                return StatusCode(500, new CopyFileResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Move/rename a file.
        /// </summary>
        /// <param name="request">Move request containing provider ID, source path, destination path, and user ID</param>
        /// <returns>Operation status</returns>
        /// <response code="200">File moved successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="404">Provider or source file not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("move")]
        [ProducesResponseType(typeof(MoveFileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MoveFileResponse>> MoveFile([FromBody] MoveFileRequest request)
        {
            try
            {
                var command = new MoveFileCommand { Request = request };
                var result = await _moveFileHandler.HandleAsync(command);

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
                _logger.LogError(ex, "Error in MoveFile endpoint");
                return StatusCode(500, new MoveFileResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Check if file/directory exists.
        /// </summary>
        /// <param name="providerId">Provider ID</param>
        /// <param name="path">File or directory path</param>
        /// <param name="userId">User ID performing the operation</param>
        /// <returns>Existence status</returns>
        /// <response code="200">Check completed successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="404">Provider not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("exists")]
        [ProducesResponseType(typeof(ExistsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExistsResponse>> CheckExists(
            [FromQuery] string providerId,
            [FromQuery] string path,
            [FromQuery] string? userId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(providerId))
                {
                    return BadRequest(new ExistsResponse
                    {
                        Success = false,
                        Message = "ProviderId is required",
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (string.IsNullOrWhiteSpace(path))
                {
                    return BadRequest(new ExistsResponse
                    {
                        Success = false,
                        Message = "Path is required",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var request = new ExistsRequest
                {
                    ProviderId = providerId,
                    Path = path,
                    UserId = userId ?? string.Empty
                };

                var command = new ExistsCommand { Request = request };
                var result = await _existsHandler.HandleAsync(command);

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
                _logger.LogError(ex, "Error in Exists endpoint");
                return StatusCode(500, new ExistsResponse
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Download a file as stream for large file handling.
        /// </summary>
        /// <param name="providerId">Provider ID</param>
        /// <param name="filePath">File path to download</param>
        /// <returns>File stream</returns>
        /// <response code="200">File streamed successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="404">Provider or file not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("stream/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadStream(
            [FromQuery] string providerId,
            [FromQuery] string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(providerId))
                {
                    return BadRequest("ProviderId is required");
                }

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return BadRequest("FilePath is required");
                }

                if (!await _fileOperationRepository.ProviderExistsAsync(providerId))
                {
                    return NotFound($"Provider '{providerId}' not found");
                }

                var stream = await _fileOperationRepository.ReadStreamAsync(providerId, filePath);
                
                var fileName = Path.GetFileName(filePath) ?? "download";
                return File(stream, "application/octet-stream", fileName);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "File not found: {FilePath}", filePath);
                return NotFound($"File not found: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading stream for {FilePath} from provider {ProviderId}",
                    filePath, providerId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Upload a file as stream for large file handling.
        /// </summary>
        /// <param name="providerId">Provider ID</param>
        /// <param name="filePath">File path to upload to</param>
        /// <param name="file">File to upload</param>
        /// <returns>Operation status</returns>
        /// <response code="200">File uploaded successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="404">Provider not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("stream/upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadStream(
            [FromQuery] string providerId,
            [FromQuery] string filePath,
            IFormFile file)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(providerId))
                {
                    return BadRequest("ProviderId is required");
                }

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return BadRequest("FilePath is required");
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest("File is required");
                }

                if (!await _fileOperationRepository.ProviderExistsAsync(providerId))
                {
                    return NotFound($"Provider '{providerId}' not found");
                }

                using var stream = file.OpenReadStream();
                await _fileOperationRepository.WriteStreamAsync(providerId, filePath, stream);
                
                return Ok(new { 
                    Success = true, 
                    Message = "File uploaded successfully",
                    FilePath = filePath,
                    Size = file.Length,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading stream to {FilePath} on provider {ProviderId}",
                    filePath, providerId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
