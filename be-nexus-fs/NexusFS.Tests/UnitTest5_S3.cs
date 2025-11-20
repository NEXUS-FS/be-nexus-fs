using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Infrastructure.Services;
using Xunit;


/** * Note: These tests require LocalStack to be running with S3 service enabled.
 * LocalStack can be started via Docker:
 */
 /*
namespace NexusFS.Tests
{
    public class S3ProviderTests : IDisposable
    {
        private readonly S3Provider _provider;
        private readonly string _bucketName;
        private readonly AmazonS3Client _setupClient;

        public S3ProviderTests()
        {
            _bucketName = $"test-bucket-{Guid.NewGuid()}";

            Console.WriteLine($"[Setup] Initializing test with Bucket: {_bucketName}");

            // 1. Setup Local Client
            var s3Config = new AmazonS3Config
            {
                ServiceURL = "http://localhost:4566",
                ForcePathStyle = true
            };
            
            try
            {
                _setupClient = new AmazonS3Client("test", "test", s3Config);
                _setupClient.PutBucketAsync(_bucketName).Wait();
                Console.WriteLine("[Setup] Bucket created successfully in LocalStack.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Setup] FATAL ERROR: Could not connect to LocalStack. {ex.Message}");
                throw;
            }

            // 2. Configure Provider..testing ..
            var config = new Dictionary<string, string>
            {
                { "serviceUrl", "http://localhost:4566" },
                { "accessKey", "test" },
                { "secretKey", "test" },
                { "bucketName", _bucketName },
                { "region", "us-east-1" }
            };

            _provider = new S3Provider("s3-test-id");
            _provider.Initialize(config).Wait();
            Console.WriteLine("[Setup] Provider initialized.");
        }

        public void Dispose()
        {
            Console.WriteLine("[Teardown] Cleaning up resources...");
            try
            {
                var objects = _setupClient.ListObjectsAsync(_bucketName).Result;
                foreach (var obj in objects.S3Objects)
                {
                    _setupClient.DeleteObjectAsync(_bucketName, obj.Key).Wait();
                }
                _setupClient.DeleteBucketAsync(_bucketName).Wait();
                Console.WriteLine("[Teardown] Bucket deleted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Teardown] Warning: Cleanup failed. {ex.Message}");
            }
            _setupClient.Dispose();
        }

        [Fact]
        public async Task WriteAndRead_ShouldPersistFile()
        {
            Console.WriteLine("--> TEST START: WriteAndRead_ShouldPersistFile");
            
            var fileName = "hello.txt";
            var content = "Hello S3 World";

            // Act
            Console.WriteLine($"Attempting to write file: {fileName}");
            await _provider.WriteFileAsync(fileName, content);
            Console.WriteLine("Write complete.");

            Console.WriteLine("Attempting to read file back...");
            var result = await _provider.ReadFileAsync(fileName);
            Console.WriteLine($"Read complete. Content: {result}");

            // Assert
            Assert.Equal(content, result);
            Console.WriteLine("--> TEST PASS");
        }

        [Fact]
        public async Task ReadFile_ShouldThrow_WhenFileDoesNotExist()
        {
            Console.WriteLine("--> TEST START: ReadFile_ShouldThrow_WhenFileDoesNotExist");
            
            var missingFile = "ghost_file.txt";
            Console.WriteLine($"Attempting to read missing file: {missingFile}");

            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                try 
                {
                    await _provider.ReadFileAsync(missingFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Caught expected exception: {ex.GetType().Name} - {ex.Message}");
                    throw; // Re-throw for Assert.ThrowsAsync to catch
                }
            });
            
            Console.WriteLine("--> TEST PASS");
        }

        [Fact]
        public async Task ListFiles_ShouldHandleFolders_Recursive()
        {
            Console.WriteLine("--> TEST START: ListFiles_ShouldHandleFolders_Recursive");

            // Arrange
            Console.WriteLine("Seeding files...");
            await _provider.WriteFileAsync("root.txt", "1");
            await _provider.WriteFileAsync("docs/report.pdf", "2");
            await _provider.WriteFileAsync("docs/2023/budget.xls", "3");

            // Act
            Console.WriteLine("Listing files in 'docs' (recursive=true)...");
            var files = await _provider.ListFilesAsync("docs", recursive: true);

            Console.WriteLine($"Found {files.Count} files:");
            foreach(var f in files) Console.WriteLine($" - {f}");

            // Assert
            Assert.Contains("docs/report.pdf", files);
            Assert.Contains("docs/2023/budget.xls", files);
            Assert.DoesNotContain("root.txt", files);
            
            Console.WriteLine("--> TEST PASS");
        }
    }
}
*/