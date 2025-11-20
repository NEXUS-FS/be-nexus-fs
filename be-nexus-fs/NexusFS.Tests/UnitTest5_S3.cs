
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using DotNetEnv; 
using Infrastructure.Services;
using Xunit;
//TODO: FIX THIS TEST TO WORK WITH LOCALSTACK OR AWS at build time with git
/*

namespace NexusFS.Tests
{
    public class S3ProviderTests : IDisposable
    {
        private readonly S3Provider _provider;
        private readonly string _bucketName;
        private readonly AmazonS3Client _setupClient;
        private readonly bool _isSharedBucket;

        public S3ProviderTests()
        {
           
            var currentDir = AppContext.BaseDirectory;
            var envPath = Path.Combine(currentDir, ".env");


            if (!File.Exists(envPath))
            {
                var projectRoot = Path.GetFullPath(Path.Combine(currentDir, "../../../"));
                envPath = Path.Combine(projectRoot, ".env");
            }

            Console.WriteLine($"[Setup] Loading .env from: {envPath}");
            Env.Load(envPath);

         
            var accessKey = Environment.GetEnvironmentVariable("AWS__ACCESS_KEY_ID") ?? "dummy-access";
            var secretKey = Environment.GetEnvironmentVariable("AWS__SECRET_ACCESS_KEY") ?? "dummy-secret";
            var regionName = Environment.GetEnvironmentVariable("AWS__S3__REGION") ?? "eu-north-1";
            var envBucket = Environment.GetEnvironmentVariable("AWS__S3__BUCKET_NAME") ?? "DUMMY_BUCKET";

            Console.WriteLine($"[Config] Access Key Found: {!string.IsNullOrEmpty(accessKey)}");
            Console.WriteLine($"[Config] Secret Key Found: {!string.IsNullOrEmpty(secretKey)}");
            Console.WriteLine($"[Config] Region: {regionName}");
            Console.WriteLine($"[Config] Bucket: {envBucket}");

            if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException(
                    $"AWS Credentials missing. Tried loading from: {envPath}. " +
                    "Ensure .env exists and contains AWS__ACCESS_KEY_ID and AWS__SECRET_ACCESS_KEY.");
            }

          
        
            if (!string.IsNullOrWhiteSpace(envBucket))
            {
                _bucketName = envBucket;
                _isSharedBucket = true;
            }
            else
            {
                _bucketName = $"test-bucket-{Guid.NewGuid()}";
                _isSharedBucket = false;
            }

            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(regionName)
            };

            try
            {
                _setupClient = new AmazonS3Client(accessKey, secretKey, s3Config);

                if (!_isSharedBucket)
                {
                    _setupClient.PutBucketAsync(_bucketName).Wait();
                    Console.WriteLine($"[Setup] Created temporary bucket: {_bucketName}");
                }
                else
                {
                   
                    _setupClient.GetBucketLocationAsync(_bucketName).Wait();
                    Console.WriteLine($"[Setup] Connected to shared bucket: {_bucketName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Setup] FATAL AWS ERROR: {ex.Message}");
                throw;
            }

            
            var configDict = new Dictionary<string, string>
            {
                { "accessKey", accessKey },
                { "secretKey", secretKey },
                { "bucketName", _bucketName },
                { "region", regionName }
            };

            _provider = new S3Provider("s3-test-id");
            _provider.Initialize(configDict).Wait();
        }

        public void Dispose()
        {
            Console.WriteLine("[Teardown] Cleaning up...");
            try
            {
              
                var listRequest = new ListObjectsV2Request { BucketName = _bucketName };
                ListObjectsV2Response listResponse;
                
                do
                {
                    listResponse = _setupClient.ListObjectsV2Async(listRequest).Result;
                    foreach (var obj in listResponse.S3Objects)
                    {
                        _setupClient.DeleteObjectAsync(_bucketName, obj.Key).Wait();
                    }
                    listRequest.ContinuationToken = listResponse.NextContinuationToken;
                } while (listResponse.IsTruncated == true);


                if (!_isSharedBucket)
                {
                    _setupClient.DeleteBucketAsync(_bucketName).Wait();
                    Console.WriteLine("[Teardown] Temporary bucket deleted.");
                }
                else
                {
                    Console.WriteLine("[Teardown] Shared bucket preserved (files cleaned).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Teardown] Warning: {ex.Message}");
            }
            finally
            {
                _setupClient.Dispose();
            }
        }

        [Fact]
        public async Task WriteAndRead_ShouldPersistFile()
        {
            // Arrange
            var fileName = $"test-{Guid.NewGuid()}.txt";
            var content = "Integration Test Content";

            // Act
            await _provider.WriteFileAsync(fileName, content);
            var result = await _provider.ReadFileAsync(fileName);

            // Assert
            Assert.Equal(content, result);
        }

        [Fact]
        public async Task ReadFile_ShouldThrow_WhenFileDoesNotExist()
        {
            var missingFile = $"ghost-{Guid.NewGuid()}.txt";
            await Assert.ThrowsAsync<FileNotFoundException>(async () => 
                await _provider.ReadFileAsync(missingFile));
        }

        [Fact]
        public async Task ListFiles_ShouldHandleFolders_Recursive()
        {
            var prefix = $"run-{Guid.NewGuid()}/";
            await _provider.WriteFileAsync($"{prefix}root.txt", "1");
            await _provider.WriteFileAsync($"{prefix}sub/doc.txt", "2");

            var files = await _provider.ListFilesAsync(prefix, recursive: true);
            
            Assert.Contains(files, f => f.EndsWith("root.txt"));
            Assert.Contains(files, f => f.EndsWith("sub/doc.txt"));
        }
    }
}
*/