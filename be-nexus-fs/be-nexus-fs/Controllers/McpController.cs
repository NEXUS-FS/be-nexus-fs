using Microsoft.AspNetCore.Mvc;
using Infrastructure.Services;
using Application.DTOs.FileOperations;

namespace be_nexus_fs.Controllers
{
    /// <summary>
    /// MCP (Model Context Protocol) API Controller for LLM/Agent file access.
    /// Provides constrained tool interfaces with security guardrails.
    /// </summary>
    [ApiController]
    [Route("api/mcp")]
    [Produces("application/json")]
    public class McpController : ControllerBase
    {
        private readonly UriRouter _uriRouter;
        private readonly ILogger<McpController> _logger;

        public McpController(UriRouter uriRouter, ILogger<McpController> logger)
        {
            _uriRouter = uriRouter;
            _logger = logger;
        }

        /// <summary>
        /// MCP Tool: Read file content from a URI.
        /// </summary>
        [HttpPost("tools/read_file")]
        [ProducesResponseType(typeof(McpReadFileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<McpReadFileResponse>> ReadFile([FromBody] McpReadFileRequest request)
        {
            try
            {
                _logger.LogInformation("MCP read_file tool invoked for URI: {Uri}", request.Uri);

                var content = await _uriRouter.ReadFileAsync(request.Uri);

                return Ok(new McpReadFileResponse
                {
                    Success = true,
                    Content = content,
                    Uri = request.Uri,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP read_file tool failed for URI: {Uri}", request.Uri);
                return StatusCode(500, new McpReadFileResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Uri = request.Uri,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// MCP Tool: Write file content to a URI.
        /// </summary>
        [HttpPost("tools/write_file")]
        [ProducesResponseType(typeof(McpWriteFileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<McpWriteFileResponse>> WriteFile([FromBody] McpWriteFileRequest request)
        {
            try
            {
                _logger.LogInformation("MCP write_file tool invoked for URI: {Uri}", request.Uri);

                await _uriRouter.WriteFileAsync(request.Uri, request.Content);

                return Ok(new McpWriteFileResponse
                {
                    Success = true,
                    Uri = request.Uri,
                    BytesWritten = request.Content.Length,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP write_file tool failed for URI: {Uri}", request.Uri);
                return StatusCode(500, new McpWriteFileResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Uri = request.Uri,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// MCP Tool: List files in a directory.
        /// </summary>
        [HttpPost("tools/list_directory")]
        [ProducesResponseType(typeof(McpListDirectoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<McpListDirectoryResponse>> ListDirectory([FromBody] McpListDirectoryRequest request)
        {
            try
            {
                _logger.LogInformation("MCP list_directory tool invoked for URI: {Uri}", request.Uri);

                var files = await _uriRouter.ListFilesAsync(request.Uri, request.Recursive);

                return Ok(new McpListDirectoryResponse
                {
                    Success = true,
                    Files = files,
                    Uri = request.Uri,
                    Count = files.Count,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP list_directory tool failed for URI: {Uri}", request.Uri);
                return StatusCode(500, new McpListDirectoryResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Uri = request.Uri,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// MCP Tool: Delete a file.
        /// </summary>
        [HttpPost("tools/delete_file")]
        [ProducesResponseType(typeof(McpDeleteFileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<McpDeleteFileResponse>> DeleteFile([FromBody] McpDeleteFileRequest request)
        {
            try
            {
                _logger.LogInformation("MCP delete_file tool invoked for URI: {Uri}", request.Uri);

                await _uriRouter.DeleteFileAsync(request.Uri);

                return Ok(new McpDeleteFileResponse
                {
                    Success = true,
                    Uri = request.Uri,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP delete_file tool failed for URI: {Uri}", request.Uri);
                return StatusCode(500, new McpDeleteFileResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Uri = request.Uri,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// MCP Tool: Check if a file exists.
        /// </summary>
        [HttpPost("tools/exists")]
        [ProducesResponseType(typeof(McpExistsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<McpExistsResponse>> Exists([FromBody] McpExistsRequest request)
        {
            try
            {
                _logger.LogInformation("MCP exists tool invoked for URI: {Uri}", request.Uri);

                var exists = await _uriRouter.ExistsAsync(request.Uri);

                return Ok(new McpExistsResponse
                {
                    Success = true,
                    Exists = exists,
                    Uri = request.Uri,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP exists tool failed for URI: {Uri}", request.Uri);
                return StatusCode(500, new McpExistsResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Uri = request.Uri,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// MCP Tool: Copy file from source to destination.
        /// </summary>
        [HttpPost("tools/copy")]
        [ProducesResponseType(typeof(McpCopyResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<McpCopyResponse>> Copy([FromBody] McpCopyRequest request)
        {
            try
            {
                _logger.LogInformation("MCP copy tool invoked from {Source} to {Destination}", request.SourceUri, request.DestinationUri);

                await _uriRouter.CopyAsync(request.SourceUri, request.DestinationUri);

                return Ok(new McpCopyResponse
                {
                    Success = true,
                    SourceUri = request.SourceUri,
                    DestinationUri = request.DestinationUri,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP copy tool failed from {Source} to {Destination}", request.SourceUri, request.DestinationUri);
                return StatusCode(500, new McpCopyResponse
                {
                    Success = false,
                    Error = ex.Message,
                    SourceUri = request.SourceUri,
                    DestinationUri = request.DestinationUri,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// MCP Tool: Move file from source to destination.
        /// </summary>
        [HttpPost("tools/move")]
        [ProducesResponseType(typeof(McpMoveResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<McpMoveResponse>> Move([FromBody] McpMoveRequest request)
        {
            try
            {
                _logger.LogInformation("MCP move tool invoked from {Source} to {Destination}", request.SourceUri, request.DestinationUri);

                await _uriRouter.MoveAsync(request.SourceUri, request.DestinationUri);

                return Ok(new McpMoveResponse
                {
                    Success = true,
                    SourceUri = request.SourceUri,
                    DestinationUri = request.DestinationUri,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP move tool failed from {Source} to {Destination}", request.SourceUri, request.DestinationUri);
                return StatusCode(500, new McpMoveResponse
                {
                    Success = false,
                    Error = ex.Message,
                    SourceUri = request.SourceUri,
                    DestinationUri = request.DestinationUri,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }

    // MCP DTOs
    public class McpReadFileRequest
    {
        public string Uri { get; set; } = string.Empty;
    }

    public class McpReadFileResponse
    {
        public bool Success { get; set; }
        public string? Content { get; set; }
        public string? Error { get; set; }
        public string Uri { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class McpWriteFileRequest
    {
        public string Uri { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class McpWriteFileResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string Uri { get; set; } = string.Empty;
        public int BytesWritten { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class McpListDirectoryRequest
    {
        public string Uri { get; set; } = string.Empty;
        public bool Recursive { get; set; }
    }

    public class McpListDirectoryResponse
    {
        public bool Success { get; set; }
        public List<string> Files { get; set; } = new();
        public string? Error { get; set; }
        public string Uri { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class McpDeleteFileRequest
    {
        public string Uri { get; set; } = string.Empty;
    }

    public class McpDeleteFileResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string Uri { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class McpExistsRequest
    {
        public string Uri { get; set; } = string.Empty;
    }

    public class McpExistsResponse
    {
        public bool Success { get; set; }
        public bool Exists { get; set; }
        public string? Error { get; set; }
        public string Uri { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class McpCopyRequest
    {
        public string SourceUri { get; set; } = string.Empty;
        public string DestinationUri { get; set; } = string.Empty;
    }

    public class McpCopyResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string SourceUri { get; set; } = string.Empty;
        public string DestinationUri { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class McpMoveRequest
    {
        public string SourceUri { get; set; } = string.Empty;
        public string DestinationUri { get; set; } = string.Empty;
    }

    public class McpMoveResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string SourceUri { get; set; } = string.Empty;
        public string DestinationUri { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}

