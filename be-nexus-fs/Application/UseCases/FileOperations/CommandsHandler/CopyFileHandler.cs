using Domain.Repositories;
using Application.UseCases.FileOperations.Commands;
using Application.DTOs.FileOperations;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.FileOperations.CommandsHandler
{
    /// <summary>
    /// Handler for copying files on a storage provider.
    /// </summary>
    public class CopyFileHandler
    {
        private readonly IFileOperationRepository _fileOperationRepository;
        private readonly ILogger<CopyFileHandler> _logger;

        public CopyFileHandler(
            IFileOperationRepository fileOperationRepository,
            ILogger<CopyFileHandler> logger)
        {
            _fileOperationRepository = fileOperationRepository;
            _logger = logger;
        }

        public async Task<CopyFileResponse> HandleAsync(CopyFileCommand command)
        {
            var request = command.Request;

            _logger.LogInformation("Copying file from {SourcePath} to {DestinationPath} on provider {ProviderId} for user {UserId}",
                request.SourcePath, request.DestinationPath, request.ProviderId, request.UserId);

            try
            {
                if (!await _fileOperationRepository.ProviderExistsAsync(request.ProviderId))
                {
                    return new CopyFileResponse
                    {
                        Success = false,
                        Message = $"Provider '{request.ProviderId}' not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                await _fileOperationRepository.CopyAsync(request.ProviderId, request.SourcePath, request.DestinationPath);

                return new CopyFileResponse
                {
                    Success = true,
                    Message = "File copied successfully",
                    SourcePath = request.SourcePath,
                    DestinationPath = request.DestinationPath,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "Source file not found: {SourcePath}", request.SourcePath);
                return new CopyFileResponse
                {
                    Success = false,
                    Message = $"Source file not found: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file from {SourcePath} to {DestinationPath} on provider {ProviderId}",
                    request.SourcePath, request.DestinationPath, request.ProviderId);
                throw;
            }
        }
    }
}

