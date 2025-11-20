using Domain.Repositories;
using Application.UseCases.FileOperations.Commands;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.FileOperations.CommandsHandler
{
    /// <summary>
    /// Handler for reading file content from a storage provider.
    /// </summary>
    public class ReadFileHandler
    {
        private readonly IFileOperationRepository _fileOperationRepository;
        private readonly ILogger<ReadFileHandler> _logger;

        public ReadFileHandler(
            IFileOperationRepository fileOperationRepository,
            ILogger<ReadFileHandler> logger)
        {
            _fileOperationRepository = fileOperationRepository;
            _logger = logger;
        }

        public async Task<ReadFileCommandResponse> HandleAsync(ReadFileCommand command)
        {
            var request = command.Request;

            _logger.LogInformation("Reading file {FilePath} from provider {ProviderId} for user {UserId}",
                request.FilePath, request.ProviderId, request.UserId);

            try
            {
                if (!await _fileOperationRepository.ProviderExistsAsync(request.ProviderId))
                {
                    return new ReadFileCommandResponse
                    {
                        Success = false,
                        Message = $"Provider '{request.ProviderId}' not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                var content = await _fileOperationRepository.ReadFileAsync(request.ProviderId, request.FilePath);

                return new ReadFileCommandResponse
                {
                    Success = true,
                    Message = "File read successfully",
                    Content = content,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "File not found: {FilePath}", request.FilePath);
                return new ReadFileCommandResponse
                {
                    Success = false,
                    Message = $"File not found: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file {FilePath} from provider {ProviderId}",
                    request.FilePath, request.ProviderId);
                throw;
            }
        }
    }
}
