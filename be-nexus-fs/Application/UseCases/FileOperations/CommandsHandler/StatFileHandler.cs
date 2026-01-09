using Domain.Repositories;
using Application.UseCases.FileOperations.Commands;
using Application.DTOs.FileOperations;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.FileOperations.CommandsHandler
{
    /// <summary>
    /// Handler for getting file/directory metadata from a storage provider.
    /// </summary>
    public class StatFileHandler
    {
        private readonly IFileOperationRepository _fileOperationRepository;
        private readonly ILogger<StatFileHandler> _logger;

        public StatFileHandler(
            IFileOperationRepository fileOperationRepository,
            ILogger<StatFileHandler> logger)
        {
            _fileOperationRepository = fileOperationRepository;
            _logger = logger;
        }

        public async Task<StatFileResponse> HandleAsync(StatFileCommand command)
        {
            var request = command.Request;

            _logger.LogInformation("Getting metadata for {Path} from provider {ProviderId} for user {UserId}",
                request.Path, request.ProviderId, request.UserId);

            try
            {
                if (!await _fileOperationRepository.ProviderExistsAsync(request.ProviderId))
                {
                    return new StatFileResponse
                    {
                        Success = false,
                        Message = $"Provider '{request.ProviderId}' not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                var metadata = await _fileOperationRepository.StatAsync(request.ProviderId, request.Path);

                return new StatFileResponse
                {
                    Success = true,
                    Message = "Metadata retrieved successfully",
                    Metadata = metadata,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metadata for {Path} from provider {ProviderId}",
                    request.Path, request.ProviderId);
                throw;
            }
        }
    }
}

