using Domain.Repositories;
using Application.UseCases.FileOperations.Commands;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.FileOperations.CommandsHandler
{
    /// <summary>
    /// Handler for deleting a file from a storage provider.
    /// </summary>
    public class DeleteFileHandler
    {
        private readonly IFileOperationRepository _fileOperationRepository;
        private readonly ILogger<DeleteFileHandler> _logger;

        public DeleteFileHandler(
            IFileOperationRepository fileOperationRepository,
            ILogger<DeleteFileHandler> logger)
        {
            _fileOperationRepository = fileOperationRepository;
            _logger = logger;
        }

        public async Task<DeleteFileCommandResponse> HandleAsync(DeleteFileCommand command)
        {
            var request = command.Request;

            _logger.LogInformation("Deleting file {FilePath} from provider {ProviderId} for user {UserId}",
                request.FilePath, request.ProviderId, request.UserId);

            try
            {
                if (!await _fileOperationRepository.ProviderExistsAsync(request.ProviderId))
                {
                    return new DeleteFileCommandResponse
                    {
                        Success = false,
                        Message = $"Provider '{request.ProviderId}' not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                await _fileOperationRepository.DeleteFileAsync(request.ProviderId, request.FilePath);

                return new DeleteFileCommandResponse
                {
                    Success = true,
                    Message = "File deleted successfully",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "File not found: {FilePath}", request.FilePath);
                return new DeleteFileCommandResponse
                {
                    Success = false,
                    Message = $"File not found: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FilePath} from provider {ProviderId}",
                    request.FilePath, request.ProviderId);
                throw;
            }
        }
    }
}
