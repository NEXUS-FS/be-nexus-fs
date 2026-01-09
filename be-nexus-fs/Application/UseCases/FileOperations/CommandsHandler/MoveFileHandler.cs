using Domain.Repositories;
using Application.UseCases.FileOperations.Commands;
using Application.DTOs.FileOperations;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.FileOperations.CommandsHandler
{
    /// <summary>
    /// Handler for moving/renaming files on a storage provider.
    /// </summary>
    public class MoveFileHandler
    {
        private readonly IFileOperationRepository _fileOperationRepository;
        private readonly ILogger<MoveFileHandler> _logger;

        public MoveFileHandler(
            IFileOperationRepository fileOperationRepository,
            ILogger<MoveFileHandler> logger)
        {
            _fileOperationRepository = fileOperationRepository;
            _logger = logger;
        }

        public async Task<MoveFileResponse> HandleAsync(MoveFileCommand command)
        {
            var request = command.Request;

            _logger.LogInformation("Moving file from {SourcePath} to {DestinationPath} on provider {ProviderId} for user {UserId}",
                request.SourcePath, request.DestinationPath, request.ProviderId, request.UserId);

            try
            {
                if (!await _fileOperationRepository.ProviderExistsAsync(request.ProviderId))
                {
                    return new MoveFileResponse
                    {
                        Success = false,
                        Message = $"Provider '{request.ProviderId}' not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                await _fileOperationRepository.MoveAsync(request.ProviderId, request.SourcePath, request.DestinationPath);

                return new MoveFileResponse
                {
                    Success = true,
                    Message = "File moved successfully",
                    SourcePath = request.SourcePath,
                    DestinationPath = request.DestinationPath,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "Source file not found: {SourcePath}", request.SourcePath);
                return new MoveFileResponse
                {
                    Success = false,
                    Message = $"Source file not found: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file from {SourcePath} to {DestinationPath} on provider {ProviderId}",
                    request.SourcePath, request.DestinationPath, request.ProviderId);
                throw;
            }
        }
    }
}

