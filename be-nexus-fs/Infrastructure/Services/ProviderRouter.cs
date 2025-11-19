using Application.DTOs;
using Application.Common;
using Infrastructure.Services.Observability;
using Infrastructure.Services.Security;
using System.Text.Json;

namespace Infrastructure.Services
{
    public class ProviderRouter
    {
        private readonly ProviderManager _providerManager;
        private readonly Logger _logger;
        private readonly AuthManager _authManager;

        public ProviderRouter(ProviderManager providerManager, Logger logger, AuthManager authManager)
        {
            _providerManager = providerManager;
            _logger = logger;
            _authManager = authManager;
        }

        public async Task<Provider> RouteToProvider(string providerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(providerId))
                {
                    _logger.LogWarning("RouteToProvider called with empty providerId", nameof(ProviderRouter));
                    throw new KeyNotFoundException("ProviderId is required");
                }

                _logger.LogInformation($"Routing to provider: {providerId}", nameof(ProviderRouter));
                var provider = await _providerManager.GetProvider(providerId);

                if (provider == null)
                {
                    _logger.LogWarning($"Provider not found: {providerId}", nameof(ProviderRouter));
                    throw new KeyNotFoundException($"Provider '{providerId}' not found");
                }

                _logger.LogInformation($"Routed to provider: {providerId} ({provider.ProviderType})", nameof(ProviderRouter));
                return provider;
            }
            catch (NotImplementedException)
            {
                // Current ProviderManager may not yet be implemented in this story.
                _logger.LogWarning($"ProviderManager.GetProvider not implemented; cannot resolve provider '{providerId}'", nameof(ProviderRouter));
                throw new KeyNotFoundException($"Provider '{providerId}' not found");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error routing to provider '{providerId}': {ex.Message}", nameof(ProviderRouter), ex);
                throw;
            }
        }

        public async Task<FileOperationResponse> ExecuteOperation(
            string providerId, string operation, Dictionary<string, object> parameters)
        { 
            var response = new FileOperationResponse
            {
                Timestamp = DateTime.UtcNow
            };

            FileOperation? parsedOperation = null;
            try
            {
                parsedOperation = ParseOperation(operation);
                response.Operation = parsedOperation;

                _logger.LogInformation($"Executing operation '{operation}' on provider '{providerId}'", nameof(ProviderRouter));

                var provider = await RouteToProvider(providerId);

                switch (parsedOperation)
                {
                    case FileOperation.Read:
                    {
                        var filePath = GetRequired<string>(parameters, "filePath");
                        var content = await provider.ReadFileAsync(filePath);
                        response.Success = true;
                        response.Content = content;
                        response.Message = "File read successfully";
                        break;
                    }
                    case FileOperation.Write:
                    {
                        var filePath = GetRequired<string>(parameters, "filePath");
                        var content = GetRequired<string>(parameters, "content");
                        await provider.WriteFileAsync(filePath, content);
                        response.Success = true;
                        response.Message = "File written successfully";
                        break;
                    }
                    case FileOperation.Delete:
                    {
                        var filePath = GetRequired<string>(parameters, "filePath");
                        await provider.DeleteFileAsync(filePath);
                        response.Success = true;
                        response.Message = "File deleted successfully";
                        break;
                    }
                    case FileOperation.List:
                    {
                        var directoryPath = GetRequired<string>(parameters, "directoryPath");
                        var recursive = GetOptional<bool>(parameters, "recursive", false);
                        var files = await provider.ListFilesAsync(directoryPath, recursive);
                        response.Success = true;
                        response.Message = "Files listed successfully";
                        response.Content = JsonSerializer.Serialize(files);
                        break;
                    }
                    default:
                    {
                        response.Success = false;
                        response.Message = $"Unsupported operation: {operation}";
                        break;
                    }
                }

                _logger.LogInformation($"Operation '{operation}' completed on provider '{providerId}' with success={response.Success}", nameof(ProviderRouter));
                return response;
            }
            catch (KeyNotFoundException knf)
            {
                response.Success = false;
                response.Message = knf.Message;
                response.Operation = parsedOperation;
                _logger.LogWarning($"Operation '{operation}' failed: {knf.Message}", nameof(ProviderRouter));
                return response;
            }
            catch (FileNotFoundException fnf)
            {
                response.Success = false;
                response.Message = $"File not found: {fnf.Message}";
                response.Operation = parsedOperation;
                _logger.LogWarning(response.Message, nameof(ProviderRouter));
                return response;
            }
            catch (ArgumentException ax)
            {
                response.Success = false;
                response.Message = ax.Message;
                response.Operation = parsedOperation;
                _logger.LogWarning($"Invalid arguments for operation '{operation}': {ax.Message}", nameof(ProviderRouter));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error executing '{operation}' on provider '{providerId}': {ex.Message}", nameof(ProviderRouter), ex);
                throw;
            } 
        }

        private static T GetRequired<T>(Dictionary<string, object> parameters, string key)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (!parameters.TryGetValue(key, out var value) || value is null)
                throw new ArgumentException($"Missing required parameter: {key}");

            try
            {
                if (value is T t) return t;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                throw new ArgumentException($"Parameter '{key}' has invalid type. Expected {typeof(T).Name}.");
            }
        }

        private static T GetOptional<T>(Dictionary<string, object> parameters, string key, T defaultValue)
        {
            if (parameters == null) return defaultValue;
            if (!parameters.TryGetValue(key, out var value) || value is null) return defaultValue;

            try
            {
                if (value is T t) return t;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        private static FileOperation ParseOperation(string operation)
        {
            if (Enum.TryParse<FileOperation>(operation, true, out var op))
            {
                return op;
            }

            // Support common aliases
            var lower = operation?.Trim().ToLowerInvariant();
            return lower switch
            {
                "readfile" => FileOperation.Read,
                "writefile" => FileOperation.Write,
                "deletefile" => FileOperation.Delete,
                "listfiles" => FileOperation.List,
                _ => throw new ArgumentException($"Unsupported operation: {operation}")
            };
        }
    }
}
