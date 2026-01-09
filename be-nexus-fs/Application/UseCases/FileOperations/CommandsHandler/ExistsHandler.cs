using Domain.Repositories;
using Application.UseCases.FileOperations.Commands;
using Application.DTOs.FileOperations;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.FileOperations.CommandsHandler
{
    /// <summary>
    /// Handler for checking if file/directory exists on a storage provider.
    /// </summary>
    public class ExistsHandler
    {
        private readonly IFileOperationRepository _fileOperationRepository;
        private readonly ILogger<ExistsHandler> _logger;

        public ExistsHandler(
            IFileOperationRepository fileOperationRepository,
            ILogger<ExistsHandler> logger)
        {
            _fileOperationRepository = fileOperationRepository;
            _logger = logger;
        }

        public async Task<ExistsResponse> HandleAsync(ExistsCommand command)
        {
            var request = command.Request;

            _logger.LogInformation("Checking if {Path} exists on provider {ProviderId} for user {UserId}",
                request.Path, request.ProviderId, request.UserId);

            try
            {
                if (!await _fileOperationRepository.ProviderExistsAsync(request.ProviderId))
                {
                    return new ExistsResponse
                    {
                        Success = false,
                        Message = $"Provider '{request.ProviderId}' not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                var exists = await _fileOperationRepository.ExistsAsync(request.ProviderId, request.Path);

                return new ExistsResponse
                {
                    Success = true,
                    Message = "Existence check completed successfully",
                    Path = request.Path,
                    Exists = exists,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of {Path} on provider {ProviderId}",
                    request.Path, request.ProviderId);
                throw;
            }
        }
    }
}

