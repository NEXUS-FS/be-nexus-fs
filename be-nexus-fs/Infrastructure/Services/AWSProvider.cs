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

            // S3 Delete is idempotent; it returns success even if file doesn't exist.
            // We accept this behavior to reduce API calls (cost optimization).
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = NormalizePath(filePath)
            };

            await _s3Client.DeleteObjectAsync(request);
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
            
            results.AddRange(response.S3Objects.Select(o => o.Key));

            if (!recursive)
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

        // --- Helpers ---

        private void EnsureInitialized()
        {
            if (_s3Client == null)
                throw new InvalidOperationException("S3Provider not initialized.");
        }

        private string GetConfigValue(Dictionary<string, string> config, string key, string defaultValue = null)
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