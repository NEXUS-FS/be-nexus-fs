using Application.Services;
using Application.UseCases.FileOperations.Commands;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.FileOperations.CommandsHandler
{
    /// <summary>
    /// Handler for writing content to a file on a storage provider.
    /// </summary>
    public class WriteFileHandler
    {
        private readonly IFileOperationService _fileOperationService;
        private readonly ILogger<WriteFileHandler> _logger;

        public WriteFileHandler(
            IFileOperationService fileOperationService,
            ILogger<WriteFileHandler> logger)
        {
            _fileOperationService = fileOperationService;
            _logger = logger;
        }

        public async Task<WriteFileCommandResponse> HandleAsync(WriteFileCommand command)
        {
            var request = command.Request;

            _logger.LogInformation("Writing file {FilePath} to provider {ProviderId} for user {UserId}",
                request.FilePath, request.ProviderId, request.UserId);

            try
            {
                if (!await _fileOperationService.ProviderExistsAsync(request.ProviderId))
                {
                    return new WriteFileCommandResponse
                    {
                        Success = false,
                        Message = $"Provider '{request.ProviderId}' not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                await _fileOperationService.WriteFileAsync(request.ProviderId, request.FilePath, request.Content);

                return new WriteFileCommandResponse
                {
                    Success = true,
                    Message = "File written successfully",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file {FilePath} to provider {ProviderId}",
                    request.FilePath, request.ProviderId);
                throw;
            }
        }
    }
}
