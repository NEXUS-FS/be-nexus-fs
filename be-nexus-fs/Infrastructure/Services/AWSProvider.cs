using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Domain.Models;

namespace Infrastructure.Services
{
    public class S3Provider : Provider
    {
        private IAmazonS3 _s3Client;
        private string _bucketName;

        public S3Provider(string providerId, string providerType, Dictionary<string, string> configuration) 
            : base(providerId, providerType, configuration)
        {
        }

        public S3Provider(string providerId) 
            : base(providerId, "S3", new Dictionary<string, string>())
        {
        }

        public override async Task Initialize(Dictionary<string, string> config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));

           //dummy for now...
            var accessKey = GetConfigValue(config, "accessKey");
            var secretKey = GetConfigValue(config, "secretKey");
            var region = GetConfigValue(config, "region", "us-east-1");
            _bucketName = GetConfigValue(config, "bucketName");
            
            // Optional: ServiceURL for LocalStack
            var serviceUrl = config.ContainsKey("serviceUrl") ? config["serviceUrl"] : null;

            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(region)
            };

            if (!string.IsNullOrEmpty(serviceUrl))
            {
                s3Config.ServiceURL = serviceUrl;
                s3Config.ForcePathStyle = true; // required for LocalStack
            }

            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            _s3Client = new AmazonS3Client(credentials, s3Config);

            await Task.CompletedTask;
        }

        public override async Task<string> ReadFileAsync(string filePath)
        {
            EnsureInitialized();

            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = NormalizePath(filePath)
                };

                using var response = await _s3Client.GetObjectAsync(request);
                using var reader = new StreamReader(response.ResponseStream);
                return await reader.ReadToEndAsync();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                throw new FileNotFoundException($"File not found in S3 bucket '{_bucketName}': {filePath}", ex);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException($"Access denied to S3 object: {filePath}", ex);
            }
        }

        public override async Task WriteFileAsync(string filePath, string content)
        {
            EnsureInitialized();

            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = NormalizePath(filePath),
                    ContentBody = content
                };

                await _s3Client.PutObjectAsync(request);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException($"Access denied writing to bucket '{_bucketName}'", ex);
            }
        }

        public override async Task DeleteFileAsync(string filePath)
        {
            EnsureInitialized();

            var normalizedPath = NormalizePath(filePath);
            
            // If path ends with "/" or looks like a directory (no file extension and path contains no dots),
            // treat it as a directory and delete all objects with that prefix
            if (normalizedPath.EndsWith("/"))
            {
                // Delete directory (all objects with this prefix)
                await DeleteDirectoryAsync(normalizedPath);
                return;
            }

            // First, delete as a file (S3 delete is idempotent, so this always succeeds)
            var deleteFileRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = normalizedPath
            };

            await _s3Client.DeleteObjectAsync(deleteFileRequest);

            // Also check if there are objects with this prefix (to handle directory deletion)
            // This allows deleting "test" to delete all files in "test/" directory
            var prefixForDirectory = normalizedPath + "/";
            await DeleteDirectoryAsync(prefixForDirectory);
        }

        private async Task DeleteDirectoryAsync(string prefix)
        {
            // Ensure prefix ends with "/"
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("/"))
            {
                prefix += "/";
            }

            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = prefix
            };

            ListObjectsV2Response response;
            do
            {
                response = await _s3Client.ListObjectsV2Async(request);
                
                if (response.S3Objects != null && response.S3Objects.Count > 0)
                {
                    // Batch delete up to 1000 objects at a time (S3 limit)
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = _bucketName,
                        Objects = response.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList(),
                        Quiet = true
                    };

                    await _s3Client.DeleteObjectsAsync(deleteRequest);
                }

                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated == true);
        }

  public override async Task<List<string>> ListFilesAsync(string directoryPath, bool recursive)
{
    EnsureInitialized();

    var prefix = NormalizePath(directoryPath);
    if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith("/"))
    {
        prefix += "/";
    }

    var request = new ListObjectsV2Request
    {
        BucketName = _bucketName,
        Prefix = prefix
    };

    if (!recursive)
    {
        request.Delimiter = "/";
    }

    var results = new List<string>();
    ListObjectsV2Response response;

    try 
    {
        do
        {
            response = await _s3Client.ListObjectsV2Async(request);
            
            if (response.S3Objects != null)
            {
                results.AddRange(response.S3Objects.Select(o => o.Key));
            }

            if (!recursive && response.CommonPrefixes != null)
            {
                results.AddRange(response.CommonPrefixes);
            }

            request.ContinuationToken = response.NextContinuationToken;

      
        } while (response.IsTruncated == true);  //this is bool?....
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return new List<string>();
    }

    return results;
}

        public override async Task<bool> TestConnectionAsync()
        {
            if (_s3Client == null) return false;

            try
            {
                // Lightweight check: Does the bucket exist and do we have access?
                return await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
            }
            catch
            {
                return false;
            }
        }

        public override async Task<FileMetadata> StatAsync(string path)
        {
            EnsureInitialized();
            var key = NormalizePath(path);

            var metadata = new FileMetadata
            {
                Path = path,
                Name = Path.GetFileName(path) ?? path,
                IsDirectory = false
            };

            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                var response = await _s3Client.GetObjectMetadataAsync(request);
                
                metadata.Exists = true;
                metadata.Size = response.ContentLength;
                metadata.ContentType = response.Headers.ContentType;
                metadata.Modified = response.LastModified;
                metadata.Created = response.LastModified; // S3 doesn't track creation separately

                return metadata;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                metadata.Exists = false;
                return metadata;
            }
        }

        public override async Task MkdirAsync(string path, bool recursive = true)
        {
            EnsureInitialized();
            // S3 doesn't have real directories, but we can create an empty object with a trailing slash
            var key = NormalizePath(path);
            if (!key.EndsWith("/"))
            {
                key += "/";
            }

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                ContentBody = string.Empty
            };

            await _s3Client.PutObjectAsync(request);
        }

        public override async Task CopyAsync(string sourcePath, string destinationPath)
        {
            EnsureInitialized();
            var sourceKey = NormalizePath(sourcePath);
            var destKey = NormalizePath(destinationPath);

            var request = new CopyObjectRequest
            {
                SourceBucket = _bucketName,
                SourceKey = sourceKey,
                DestinationBucket = _bucketName,
                DestinationKey = destKey
            };

            try
            {
                await _s3Client.CopyObjectAsync(request);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new FileNotFoundException($"Source file not found: {sourcePath}", ex);
            }
        }

        public override async Task MoveAsync(string sourcePath, string destinationPath)
        {
            // S3 doesn't have a native move operation, so we copy then delete
            await CopyAsync(sourcePath, destinationPath);
            await DeleteFileAsync(sourcePath);
        }

        public override async Task<bool> ExistsAsync(string path)
        {
            EnsureInitialized();
            var key = NormalizePath(path);

            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public override async Task<Stream> ReadStreamAsync(string filePath)
        {
            EnsureInitialized();

            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = NormalizePath(filePath)
                };

                var response = await _s3Client.GetObjectAsync(request);
                // Return the response stream - caller is responsible for disposing
                return response.ResponseStream;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                throw new FileNotFoundException($"File not found in S3 bucket '{_bucketName}': {filePath}", ex);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException($"Access denied to S3 object: {filePath}", ex);
            }
        }

        public override async Task WriteStreamAsync(string filePath, Stream content)
        {
            EnsureInitialized();

            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = NormalizePath(filePath),
                    InputStream = content
                };

                await _s3Client.PutObjectAsync(request);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException($"Access denied writing to bucket '{_bucketName}'", ex);
            }
        }

        // --- Helpers ---

        private void EnsureInitialized()
        {
            if (_s3Client == null)
                throw new InvalidOperationException("S3Provider not initialized.");
        }

        private string GetConfigValue(Dictionary<string, string> config, string key, string defaultValue = "")
        {
            if (config.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            if (defaultValue != null) return defaultValue;
            throw new ArgumentException($"Configuration missing required key: {key}");
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            // S3 uses forward slashes, no leading slash usually required
            return path.Replace("\\", "/").TrimStart('/');
        }
    }
}