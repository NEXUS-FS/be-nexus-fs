using Domain.Repositories;
using Application.UseCases.FileOperations.Commands;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.FileOperations.CommandsHandler
{
    /// <summary>
    /// Handler for listing files in a directory on a storage provider.
    /// </summary>
    public class ListFilesHandler
    {
        private readonly IFileOperationRepository _fileOperationRepository;
        private readonly ILogger<ListFilesHandler> _logger;

        public ListFilesHandler(
            IFileOperationRepository fileOperationRepository,
            ILogger<ListFilesHandler> logger)
        {
            _fileOperationRepository = fileOperationRepository;
            _logger = logger;
        }

        public async Task<ListFilesCommandResponse> HandleAsync(ListFilesCommand command)
        {
            var request = command.Request;

            _logger.LogInformation("Listing files in {DirectoryPath} from provider {ProviderId} for user {UserId}",
                request.DirectoryPath, request.ProviderId, request.UserId);

            try
            {
                if (!await _fileOperationRepository.ProviderExistsAsync(request.ProviderId))
                {
                    return new ListFilesCommandResponse
                    {
                        Success = false,
                        Message = $"Provider '{request.ProviderId}' not found",
                        Timestamp = DateTime.UtcNow
                    };
                }

                var files = await _fileOperationRepository.ListFilesAsync(request.ProviderId, request.DirectoryPath, request.Recursive);

                return new ListFilesCommandResponse
                {
                    Success = true,
                    Message = "Files listed successfully",
                    Files = files,
                    DirectoryPath = request.DirectoryPath,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogWarning(ex, "Directory not found: {DirectoryPath}", request.DirectoryPath);
                return new ListFilesCommandResponse
                {
                    Success = false,
                    Message = $"Directory not found: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files in {DirectoryPath} from provider {ProviderId}",
                    request.DirectoryPath, request.ProviderId);
                throw;
            }
        }
    }
}
