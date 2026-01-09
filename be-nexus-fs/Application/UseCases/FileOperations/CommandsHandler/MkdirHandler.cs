using Domain.Repositories;
using Application.UseCases.FileOperations.Commands;
using Application.DTOs.FileOperations;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.FileOperations.CommandsHandler
{
    /// <summary>
    /// Handler for creating directories on a storage provider.
    /// </summary>
    public class MkdirHandler
    {
        private readonly IFileOperationRepository _fileOperationRepository;
        private readonly ILogger<MkdirHandler> _logger;

        public MkdirHandler(
            IFileOperationRepository fileOperationRepository,
            ILogger<MkdirHandler> logger)
        {
            _fileOperationRepository = fileOperationRepository;
            _logger = logger;
        }

        public async Task<MkdirResponse> HandleAsync(MkdirCommand command)
        {
            var request = command.Request;

            _logger.LogInformation("Creating directory {Path} on provider {ProviderId} for user {UserId}",
                request.Path, request.ProviderId, request.UserId);

            try
            {
                if (!await _fileOperationRepository.ProviderExistsAsync(request.ProviderId))
                {
                    return new MkdirResponse
                    {
                        Success = false,
                        Message = $"Provider '{request.ProviderId}' not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                await _fileOperationRepository.MkdirAsync(request.ProviderId, request.Path, request.Recursive);

                return new MkdirResponse
                {
                    Success = true,
                    Message = "Directory created successfully",
                    Path = request.Path,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating directory {Path} on provider {ProviderId}",
                    request.Path, request.ProviderId);
                throw;
            }
        }
    }
}

