using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Infrastructure.Services;
using Xunit;

namespace NexusFS.Tests
{
    public class LocalProviderTests : IDisposable
    {
        private readonly string _tempBasePath;
        private readonly LocalProvider _provider;

        public LocalProviderTests()
        {
            // setup: Create a unique temp directory for this test instance
            _tempBasePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            
            _provider = new LocalProvider("test-local");
            var config = new Dictionary<string, string> { { "basePath", _tempBasePath } };
            
            // initialize synchronously for test setup context
            _provider.Initialize(config).Wait();
        }

        public void Dispose()
        {
            // teardown: Clean up temp directory
            if (Directory.Exists(_tempBasePath))
            {
                Directory.Delete(_tempBasePath, true);
            }
        }

        [Fact]
        public async Task Initialize_ShouldCreateDirectory_WhenItDoesNotExist()
        {
            var newPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var provider = new LocalProvider("init-test");
            
            await provider.Initialize(new Dictionary<string, string> { { "basePath", newPath } });

            Assert.True(Directory.Exists(newPath));
            
            // cleanup
            Directory.Delete(newPath, true);
        }

        [Fact]
        public async Task WriteFileAsync_ShouldCreateFileAndSubdirectories()
        {
            string filePath = "subfolder/test.txt";
            string content = "Hello World";

            await _provider.WriteFileAsync(filePath, content);

            string expectedFullPath = Path.Combine(_tempBasePath, filePath);
            Assert.True(File.Exists(expectedFullPath));
            Assert.Equal(content, await File.ReadAllTextAsync(expectedFullPath));
        }

        [Fact]
        public async Task ReadFileAsync_ShouldReturnContent_WhenFileExists()
        {
            string filePath = "read.txt";
            string content = "Read Me";
            await File.WriteAllTextAsync(Path.Combine(_tempBasePath, filePath), content);

            var result = await _provider.ReadFileAsync(filePath);

            Assert.Equal(content, result);
        }

        [Fact]
        public async Task DeleteFileAsync_ShouldRemoveFile()
        {
            string filePath = "delete.txt";
            string fullPath = Path.Combine(_tempBasePath, filePath);
            await File.WriteAllTextAsync(fullPath, "content");

            await _provider.DeleteFileAsync(filePath);

            Assert.False(File.Exists(fullPath));
        }

        [Fact]
        public async Task ListFilesAsync_ShouldListRecursively_WhenRecursiveIsTrue()
        {
            // Arrange
            await _provider.WriteFileAsync("root.txt", "1");
            await _provider.WriteFileAsync("level1/child.txt", "2");
            await _provider.WriteFileAsync("level1/level2/grandchild.txt", "3");

            // Act
            var files = await _provider.ListFilesAsync("", recursive: true);

            // Assert
            Assert.Equal(3, files.Count);
            Assert.Contains("root.txt", files); // Note: Depends on OS separators, normalized in implementation
            Assert.Contains("level1/child.txt", files.Select(f => f.Replace("\\", "/"))); 
        }

        [Fact]
        public async Task ListFilesAsync_ShouldListTopOnly_WhenRecursiveIsFalse()
        {
            // Arrange
            await _provider.WriteFileAsync("root.txt", "1");
            await _provider.WriteFileAsync("sub/child.txt", "2");

            // Act
            var files = await _provider.ListFilesAsync("", recursive: false);

            // Assert
            Assert.Single(files);
            Assert.Contains("root.txt", files);
        }

        [Fact]
        public async Task Security_ShouldPreventPathTraversal()
        {
            // Attempt to write to a file outside the base path (e.g., temp root)
            string badPath = "../hacked.txt";

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                _provider.WriteFileAsync(badPath, "hacked"));
        
         Console.WriteLine("Passed test!");
        }
    }
   
}