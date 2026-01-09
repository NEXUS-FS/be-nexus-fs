using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using Infrastructure.Services.Observability;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Linq;
using System.Threading;
using System.IO;
using System.Text;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace Infrastructure.Services
{
    /// <summary>
    /// Google Drive implementation of the Provider abstraction.
    /// Handles OAuth2, path-to-ID resolution (since Drive is ID-based),
    /// and basic file CRUD operations with retry/backoff and caching.
    /// </summary>
    public class GoogleDriveProvider : Provider
    {
        public const string FolderMimeType = "application/vnd.google-apps.folder";

        private readonly IMemoryCache _pathCache;
        private readonly Logger? _logger;
        private IGoogleDriveClient? _client;
        private string _rootFolderId = "root";
        private TimeSpan _pathCacheTtl = TimeSpan.FromMinutes(10);
        private string _applicationName = "NexusFS";

        public GoogleDriveProvider(string providerId)
            : this(providerId, null, new MemoryCache(new MemoryCacheOptions()), null)
        {
        }

        // Internal/testing-friendly constructor
        public GoogleDriveProvider(
            string providerId,
            IGoogleDriveClient? driveClient,
            IMemoryCache? memoryCache = null,
            Logger? logger = null)
            : base(providerId, "GoogleDrive", new Dictionary<string, string>())
        {
            _client = driveClient;
            _pathCache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());
            _logger = logger;
        }

        public override async Task Initialize(Dictionary<string, string> config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));

            _rootFolderId = config.ContainsKey("rootFolderId") && !string.IsNullOrWhiteSpace(config["rootFolderId"])
                ? config["rootFolderId"]
                : "root";

            _applicationName = config.ContainsKey("applicationName") && !string.IsNullOrWhiteSpace(config["applicationName"])
                ? config["applicationName"]
                : "NexusFS";

            if (config.TryGetValue("pathCacheTtlSeconds", out var ttlRaw) && int.TryParse(ttlRaw, out var ttlSeconds) && ttlSeconds > 0)
            {
                _pathCacheTtl = TimeSpan.FromSeconds(ttlSeconds);
            }

            if (_client != null)
            {
                return; // Testing/overridden client injected
            }

            var credential = await BuildCredentialAsync(config);
            var driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _applicationName
            });

            _client = new GoogleDriveApiClient(driveService);
        }

        public override async Task<string> ReadFileAsync(string filePath)
        {
            EnsureReady();
            var fileId = await ResolvePathToIdAsync(filePath, isFile: true, createMissingFolders: false);
            if (fileId == null)
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var stream = await ExecuteWithRetry(() => _client!.DownloadFileAsync(fileId));
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        public override async Task WriteFileAsync(string filePath, string content)
        {
            EnsureReady();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));
            }

            var normalized = NormalizePath(filePath);
            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                throw new ArgumentException("File path must include a filename.", nameof(filePath));
            }

            var fileName = segments.Last();
            var parentPath = string.Join('/', segments.Take(segments.Length - 1));
            var parentId = await ResolvePathToIdAsync(parentPath, isFile: false, createMissingFolders: true) ?? _rootFolderId;

            var existingFile = await ExecuteWithRetry(() => _client!.FindItemAsync(parentId, fileName, expectedMime: null));
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

            string? fileId;
            if (existingFile == null)
            {
                fileId = await ExecuteWithRetry(() => _client!.UploadFileAsync(parentId, fileName, stream));
            }
            else
            {
                fileId = await ExecuteWithRetry(() => _client!.UpdateFileAsync(existingFile.Id, stream));
            }

            if (!string.IsNullOrWhiteSpace(fileId))
            {
                var fullPath = string.IsNullOrWhiteSpace(parentPath) ? fileName : $"{parentPath}/{fileName}";
                _pathCache.Set(fullPath, fileId, _pathCacheTtl);
            }
        }

        public override async Task DeleteFileAsync(string filePath)
        {
            EnsureReady();
            var fileId = await ResolvePathToIdAsync(filePath, isFile: true, createMissingFolders: false);
            if (fileId == null)
            {
                return; // idempotent
            }

            await ExecuteWithRetry(() => _client!.DeleteAsync(fileId));
            _pathCache.Remove(NormalizePath(filePath));
        }

        public override async Task<List<string>> ListFilesAsync(string directoryPath, bool recursive)
        {
            EnsureReady();
            var folderId = await ResolvePathToIdAsync(directoryPath, isFile: false, createMissingFolders: false);
            if (folderId == null)
            {
                return new List<string>();
            }

            var results = new List<string>();
            var queue = new Queue<(string folderId, string relativePath)>();
            queue.Enqueue((folderId, NormalizePath(directoryPath)));

            while (queue.Count > 0)
            {
                var (currentFolderId, currentPath) = queue.Dequeue();
                var children = await ExecuteWithRetry(() => _client!.ListChildrenAsync(currentFolderId));

                foreach (var item in children)
                {
                    var relative = string.IsNullOrWhiteSpace(currentPath) ? item.Name : $"{currentPath}/{item.Name}";
                    if (item.MimeType == FolderMimeType)
                    {
                        if (recursive)
                        {
                            queue.Enqueue((item.Id, relative));
                        }
                    }
                    results.Add(relative);
                }
            }

            return results;
        }

        public override async Task<bool> TestConnectionAsync()
        {
            if (_client == null) return false;

            try
            {
                // Lightweight call: list a single item from root
                var rootChildren = await ExecuteWithRetry(() => _client.ListChildrenAsync(_rootFolderId));
                return rootChildren != null;
            }
            catch
            {
                return false;
            }
        }

        // --- Helpers ---

        private async Task<ICredential> BuildCredentialAsync(Dictionary<string, string> config)
        {
            var clientId = Require(config, "clientId");
            var clientSecret = Require(config, "clientSecret");
            var refreshToken = Require(config, "refreshToken");
            var accessToken = config.ContainsKey("accessToken") ? config["accessToken"] : null;

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[] { DriveService.Scope.Drive }
            });

            var token = new TokenResponse
            {
                RefreshToken = refreshToken,
                AccessToken = accessToken
            };

            var credential = new UserCredential(flow, "nexusfs-user", token);

            // Ensure we have a valid access token
            await credential.RefreshTokenAsync(CancellationToken.None);
            return credential;
        }

        private void EnsureReady()
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Google Drive provider is not initialized.");
            }
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            return path.Replace("\\", "/").Trim().Trim('/');
        }

        private string Require(Dictionary<string, string> config, string key)
        {
            if (config.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            throw new ArgumentException($"Configuration missing required key: {key}");
        }

        private async Task<string?> ResolvePathToIdAsync(string path, bool isFile, bool createMissingFolders)
        {
            var normalized = NormalizePath(path);
            if (string.IsNullOrEmpty(normalized))
            {
                return _rootFolderId;
            }

            if (_pathCache.TryGetValue(normalized, out string? cachedId) && !string.IsNullOrEmpty(cachedId))
            {
                return cachedId;
            }

            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentId = _rootFolderId;
            var currentPath = string.Empty;

            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                var isLast = i == segments.Length - 1;
                var expectFolder = !isLast || !isFile;
                var cacheKey = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";

                if (_pathCache.TryGetValue(cacheKey, out string? cachedSegmentId) && !string.IsNullOrEmpty(cachedSegmentId))
                {
                    currentId = cachedSegmentId;
                    currentPath = cacheKey;
                    continue;
                }

                var found = await ExecuteWithRetry(() => _client!.FindItemAsync(currentId, segment, expectFolder ? FolderMimeType : null));

                if (found == null)
                {
                    if (expectFolder && createMissingFolders)
                    {
                        var newFolderId = await ExecuteWithRetry(() => _client!.CreateFolderAsync(currentId, segment));
                        if (string.IsNullOrEmpty(newFolderId))
                        {
                            return null;
                        }
                        currentId = newFolderId;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(found.Id))
                    {
                        return null;
                    }
                    currentId = found.Id;
                }

                if (!string.IsNullOrEmpty(currentId))
                {
                    _pathCache.Set(cacheKey, currentId, _pathCacheTtl);
                }
                currentPath = cacheKey;
            }

            if (!string.IsNullOrEmpty(currentId))
            {
                _pathCache.Set(normalized, currentId, _pathCacheTtl);
            }
            return currentId;
        }

        private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action)
        {
            var delay = TimeSpan.FromSeconds(1);
            const int maxAttempts = 5;
            Exception? lastError = null;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (GoogleApiException ex) when (ShouldRetry(ex) && attempt < maxAttempts)
                {
                    _logger?.LogWarning($"Google API rate limit hit (attempt {attempt}): {ex.Message}", nameof(GoogleDriveProvider));
                    lastError = ex;
                }
                catch (HttpRequestException ex) when (attempt < maxAttempts)
                {
                    lastError = ex;
                }

                await Task.Delay(delay);
                delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
            }

            throw lastError ?? new InvalidOperationException("Operation failed after retries.");
        }

        private async Task ExecuteWithRetry(Func<Task> action)
        {
            await ExecuteWithRetry(async () =>
            {
                await action();
                return true;
            });
        }

        private bool ShouldRetry(GoogleApiException ex)
        {
            if (ex.HttpStatusCode == HttpStatusCode.TooManyRequests ||
                ex.HttpStatusCode == HttpStatusCode.ServiceUnavailable ||
                ex.HttpStatusCode == HttpStatusCode.InternalServerError)
            {
                return true;
            }

            if (ex.HttpStatusCode == HttpStatusCode.Forbidden && ex.Error?.Errors != null)
            {
                return ex.Error.Errors.Any(e =>
                    string.Equals(e.Reason, "rateLimitExceeded", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(e.Reason, "userRateLimitExceeded", StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }
    }

    /// <summary>
    /// Minimal abstraction to allow mocking Google Drive operations in tests.
    /// </summary>
    public interface IGoogleDriveClient
    {
        Task<DriveFile?> FindItemAsync(string parentId, string name, string? expectedMime);
        Task<string> CreateFolderAsync(string parentId, string name);
        Task<string> UploadFileAsync(string parentId, string name, Stream content);
        Task<string> UpdateFileAsync(string fileId, Stream content);
        Task<Stream> DownloadFileAsync(string fileId);
        Task DeleteAsync(string fileId);
        Task<IList<DriveFile>> ListChildrenAsync(string parentId);
    }

    internal class GoogleDriveApiClient : IGoogleDriveClient
    {
        private readonly DriveService _service;

        public GoogleDriveApiClient(DriveService service)
        {
            _service = service;
        }

        public async Task<DriveFile?> FindItemAsync(string parentId, string name, string? expectedMime)
        {
            var request = _service.Files.List();
            request.Q = $"'{Escape(parentId)}' in parents and name = '{Escape(name)}' and trashed = false";
            request.Fields = "files(id, name, mimeType)";
            request.Spaces = "drive";

            var response = await request.ExecuteAsync();
            var match = response.Files?.FirstOrDefault(f =>
                expectedMime == null || string.Equals(f.MimeType, expectedMime, StringComparison.OrdinalIgnoreCase));
            return match;
        }

        public async Task<string> CreateFolderAsync(string parentId, string name)
        {
            var metadata = new DriveFile
            {
                Name = name,
                MimeType = GoogleDriveProvider.FolderMimeType,
                Parents = new List<string> { parentId }
            };

            var request = _service.Files.Create(metadata);
            request.Fields = "id";
            var created = await request.ExecuteAsync();
            return created.Id;
        }

        public async Task<string> UploadFileAsync(string parentId, string name, Stream content)
        {
            var metadata = new DriveFile
            {
                Name = name,
                Parents = new List<string> { parentId }
            };

            var upload = _service.Files.Create(metadata, content, "application/octet-stream");
            upload.Fields = "id";

            var result = await upload.UploadAsync();
            if (result.Status != UploadStatus.Completed)
            {
                throw new IOException($"Failed to upload file to Google Drive: {result.Exception?.Message}");
            }

            return upload.ResponseBody.Id;
        }

        public async Task<string> UpdateFileAsync(string fileId, Stream content)
        {
            var update = _service.Files.Update(new DriveFile(), fileId, content, "application/octet-stream");
            update.Fields = "id";
            var result = await update.UploadAsync();
            if (result.Status != UploadStatus.Completed)
            {
                throw new IOException($"Failed to update file in Google Drive: {result.Exception?.Message}");
            }
            return update.ResponseBody.Id;
        }

        public async Task<Stream> DownloadFileAsync(string fileId)
        {
            var stream = new MemoryStream();
            var request = _service.Files.Get(fileId);
            await request.DownloadAsync(stream);
            stream.Position = 0;
            return stream;
        }

        public async Task DeleteAsync(string fileId)
        {
            var request = _service.Files.Delete(fileId);
            await request.ExecuteAsync();
        }

        public async Task<IList<DriveFile>> ListChildrenAsync(string parentId)
        {
            var request = _service.Files.List();
            request.Q = $"'{Escape(parentId)}' in parents and trashed = false";
            request.Fields = "files(id, name, mimeType)";
            request.Spaces = "drive";
            var response = await request.ExecuteAsync();
            return response.Files ?? new List<DriveFile>();
        }

        private static string Escape(string input) => input.Replace("'", "\\'");
    }
}

