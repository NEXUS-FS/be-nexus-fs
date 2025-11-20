using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Infrastructure.Services;
using Xunit;
using System.IO;

namespace NexusFS.Tests
{
    public class MemoryProviderTests
    {
        private readonly MemoryProvider _provider;

        public MemoryProviderTests()
        {
            _provider = new MemoryProvider("mem-test");
            _provider.Initialize(new Dictionary<string, string>()).Wait();
        }

        [Fact]
        public async Task WriteAndRead_ShouldPersistInMemory()
        {
            string path = "folder/doc.txt";
            string content = "Memory Content";

            await _provider.WriteFileAsync(path, content);
            var result = await _provider.ReadFileAsync(path);

            Assert.Equal(content, result);
        }

        [Fact]
        public async Task Delete_ShouldRemoveFromDictionary()
        {
            string path = "temp.txt";
            await _provider.WriteFileAsync(path, "data");

            await _provider.DeleteFileAsync(path);

            await Assert.ThrowsAsync<FileNotFoundException>(async () => 
                await _provider.ReadFileAsync(path));
        }

        [Fact]
        public async Task ListFiles_Recursive_ShouldReturnAllDescendants()
        {
            // Arrange
            await _provider.WriteFileAsync("logs/today/log1.txt", "A");
            await _provider.WriteFileAsync("logs/today/log2.txt", "B");
            await _provider.WriteFileAsync("logs/archive/old.txt", "C");
            await _provider.WriteFileAsync("other/data.txt", "D");

            // Act
            var results = await _provider.ListFilesAsync("logs", recursive: true);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Contains("logs/today/log1.txt", results);
            Assert.Contains("logs/archive/old.txt", results);
            Assert.DoesNotContain("other/data.txt", results);
        }

        [Fact]
        public async Task ListFiles_NonRecursive_ShouldReturnOnlyDirectChildren()
        {
            // Arrange
            await _provider.WriteFileAsync("root/file1.txt", "A");
            await _provider.WriteFileAsync("root/sub/file2.txt", "B"); // Deep file

            // Act
            var results = await _provider.ListFilesAsync("root", recursive: false);

            // Assert
            Assert.Single(results);
            Assert.Contains("root/file1.txt", results);
            Assert.DoesNotContain("root/sub/file2.txt", results);

            Console.WriteLine("Passed test!");
        }

        [Fact]
        public async Task ThreadSafety_ShouldHandleConcurrentWrites()
        {
            // Arrange
            int fileCount = 100;
            var tasks = new List<Task>();

            // Act: Write 100 files in parallel
            for (int i = 0; i < fileCount; i++)
            {
                int id = i; // capture loop variable
                tasks.Add(Task.Run(async () => 
                {
                    await _provider.WriteFileAsync($"concurrency/file{id}.txt", $"Content {id}");
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            var files = await _provider.ListFilesAsync("concurrency", true);
            Assert.Equal(fileCount, files.Count);
        }
        
    }
}