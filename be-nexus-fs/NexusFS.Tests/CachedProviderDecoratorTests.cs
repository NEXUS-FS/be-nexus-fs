using Domain.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Decorators;
using Infrastructure.Services.Observability;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace NexusFS.Tests;

public class CachedProviderDecoratorTests : IDisposable
{
    private readonly string _tempBasePath;
    private readonly LocalProvider _localProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<IAuditLogRepository> _mockAuditLogRepository;
    private readonly Logger _logger;
    private readonly CachedProviderDecorator _cachedProvider;

    public CachedProviderDecoratorTests()
    {
        // Setup: Create a unique temp directory for this test instance
        _tempBasePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Initialize LocalProvider
        _localProvider = new LocalProvider("test-local");
        var config = new Dictionary<string, string> { { "basePath", _tempBasePath } };
        _localProvider.Initialize(config).Wait();

        // Setup memory cache
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Setup logger with mocked repository
        _mockAuditLogRepository = new Mock<IAuditLogRepository>();
        _logger = new Logger(_mockAuditLogRepository.Object);

        // Create CachedProviderDecorator wrapping LocalProvider
        _cachedProvider = new CachedProviderDecorator(_localProvider, _memoryCache, _logger);
    }

    public void Dispose()
    {
        // Teardown: Clean up temp directory
        if (Directory.Exists(_tempBasePath))
        {
            Directory.Delete(_tempBasePath, true);
        }

        _memoryCache.Dispose();
    }


    [Fact]
    public async Task CachedProviderDecorator_ShouldWrapLocalProvider_Successfully()
    {
        // Arrange
        string filePath = "test.txt";
        string content = "Test content";

        // Act: Write using cached provider
        await _cachedProvider.WriteFileAsync(filePath, content);

        // Assert: Verify the file was written (delegated to LocalProvider)
        string expectedFullPath = Path.Combine(_tempBasePath, filePath);
        Assert.True(File.Exists(expectedFullPath));
        Assert.Equal(content, await File.ReadAllTextAsync(expectedFullPath));

        // Verify decorator properties match wrapped provider
        Assert.Equal(_localProvider.ProviderId, _cachedProvider.ProviderId);
        Assert.Equal(_localProvider.ProviderType, _cachedProvider.ProviderType);
        Assert.NotNull(_cachedProvider.DecoratedProvider);
        Assert.Equal(_localProvider, _cachedProvider.DecoratedProvider);
    }

    [Fact]
    public async Task CachedProviderDecorator_ShouldDelegateReadOperations_ToLocalProvider()
    {
        // Arrange
        string filePath = "read-test.txt";
        string content = "Read test content";
        await _localProvider.WriteFileAsync(filePath, content);

        // Act: Read using cached provider (first call - cache miss)
        var result = await _cachedProvider.ReadFileAsync(filePath);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public async Task ReadFileAsync_ShouldLogCacheMiss_OnFirstRead()
    {
        // Arrange
        string filePath = "cache-miss-test.txt";
        string content = "Cache miss test";
        await _localProvider.WriteFileAsync(filePath, content);

        // Capture console output to verify logging
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act: First read (should be cache miss)
            await _cachedProvider.ReadFileAsync(filePath);

            // Assert: Verify cache miss was logged
            var output = stringWriter.ToString();
            Assert.Contains("Cache MISS", output);
            Assert.Contains(filePath, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task ReadFileAsync_ShouldLogCacheHit_OnSecondRead()
    {
        // Arrange
        string filePath = "cache-hit-test.txt";
        string content = "Cache hit test";
        await _localProvider.WriteFileAsync(filePath, content);

        // First read to populate cache
        await _cachedProvider.ReadFileAsync(filePath);

        // Capture console output to verify logging
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act: Second read (should be cache hit)
            await _cachedProvider.ReadFileAsync(filePath);

            // Assert: Verify cache hit was logged
            var output = stringWriter.ToString();
            Assert.Contains("Cache HIT", output);
            Assert.Contains(filePath, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task ListFilesAsync_ShouldLogCacheMiss_OnFirstCall()
    {
        // Arrange
        await _localProvider.WriteFileAsync("file1.txt", "content1");
        await _localProvider.WriteFileAsync("file2.txt", "content2");

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act: First list (should be cache miss)
            await _cachedProvider.ListFilesAsync("", recursive: true);

            // Assert: Verify cache miss was logged
            var output = stringWriter.ToString();
            Assert.Contains("Cache MISS", output);
            Assert.Contains("directory listing", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task ListFilesAsync_ShouldLogCacheHit_OnSecondCall()
    {
        // Arrange
        await _localProvider.WriteFileAsync("file1.txt", "content1");

        // First list to populate cache
        await _cachedProvider.ListFilesAsync("", recursive: true);

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act: Second list (should be cache hit)
            await _cachedProvider.ListFilesAsync("", recursive: true);

            // Assert: Verify cache hit was logged
            var output = stringWriter.ToString();
            Assert.Contains("Cache HIT", output);
            Assert.Contains("directory listing", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task WriteFileAsync_ShouldInvalidateCache_AfterWrite()
    {
        // Arrange
        string filePath = "invalidate-test.txt";
        string initialContent = "Initial content";
        string updatedContent = "Updated content";

        // Write initial file
        await _localProvider.WriteFileAsync(filePath, initialContent);

        // Read to populate cache
        var firstRead = await _cachedProvider.ReadFileAsync(filePath);
        Assert.Equal(initialContent, firstRead);

        // Verify cache is populated by reading again (should be cache hit)
        var cachedRead = await _cachedProvider.ReadFileAsync(filePath);
        Assert.Equal(initialContent, cachedRead);

        // Act: Write new content (should invalidate cache)
        await _cachedProvider.WriteFileAsync(filePath, updatedContent);

        // Assert: Next read should get fresh content (not cached)
        var afterWriteRead = await _cachedProvider.ReadFileAsync(filePath);
        Assert.Equal(updatedContent, afterWriteRead);
    }

    [Fact]
    public async Task WriteFileAsync_ShouldLogCacheInvalidation()
    {
        // Arrange
        string filePath = "invalidate-log-test.txt";
        string content = "Test content";

        // Write initial file and read to populate cache
        await _localProvider.WriteFileAsync(filePath, content);
        await _cachedProvider.ReadFileAsync(filePath);

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act: Write file (should invalidate cache)
            await _cachedProvider.WriteFileAsync(filePath, "new content");

            // Assert: Verify cache invalidation was logged
            var output = stringWriter.ToString();
            Assert.Contains("cache invalidated", output);
            Assert.Contains(filePath, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldInvalidateCache()
    {
        // Arrange
        string filePath = "delete-invalidate-test.txt";
        string content = "Content to delete";

        // Write and read to populate cache
        await _localProvider.WriteFileAsync(filePath, content);
        await _cachedProvider.ReadFileAsync(filePath);

        // Act: Delete file (should invalidate cache)
        await _cachedProvider.DeleteFileAsync(filePath);

        // Assert: File should not exist
        string expectedFullPath = Path.Combine(_tempBasePath, filePath);
        Assert.False(File.Exists(expectedFullPath));

        // Verify cache is invalidated (attempting to read should throw)
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _cachedProvider.ReadFileAsync(filePath));
    }

    [Fact]
    public async Task ListFilesAsync_ShouldCacheResults_WithTwoMinuteTTL()
    {
        // Arrange
        await _localProvider.WriteFileAsync("file1.txt", "content1");
        await _localProvider.WriteFileAsync("file2.txt", "content2");

        // Act: First call (cache miss)
        var firstResult = await _cachedProvider.ListFilesAsync("", recursive: true);
        Assert.Equal(2, firstResult.Count);

        // Add a new file to the underlying provider
        await _localProvider.WriteFileAsync("file3.txt", "content3");

        // Act: Second call (should return cached result, not include file3)
        var secondResult = await _cachedProvider.ListFilesAsync("", recursive: true);

        // Assert: Should return cached result (2 files, not 3)
        Assert.Equal(2, secondResult.Count);
        Assert.DoesNotContain("file3.txt", secondResult);
    }

    [Fact]
    public async Task ListFilesAsync_ShouldRespectRecursiveParameter_InCacheKey()
    {
        // Arrange
        await _localProvider.WriteFileAsync("root.txt", "content1");
        await _localProvider.WriteFileAsync("sub/file.txt", "content2");

        // Act: List with recursive=true
        var recursiveResult = await _cachedProvider.ListFilesAsync("", recursive: true);
        Assert.Equal(2, recursiveResult.Count);

        // Act: List with recursive=false (different cache key)
        var nonRecursiveResult = await _cachedProvider.ListFilesAsync("", recursive: false);

        // Assert: Should return different results (non-recursive should only have root.txt)
        Assert.Single(nonRecursiveResult);
        Assert.Contains("root.txt", nonRecursiveResult);
        Assert.DoesNotContain("sub/file.txt", nonRecursiveResult);
    }

    [Fact]
    public async Task ListFilesAsync_ShouldCacheSeparately_ForDifferentDirectories()
    {
        // Arrange
        await _localProvider.WriteFileAsync("dir1/file1.txt", "content1");
        await _localProvider.WriteFileAsync("dir2/file2.txt", "content2");

        // Act: List different directories
        var dir1Result = await _cachedProvider.ListFilesAsync("dir1", recursive: true);
        var dir2Result = await _cachedProvider.ListFilesAsync("dir2", recursive: true);

        // Assert: Should return different results
        Assert.Single(dir1Result);
        Assert.Single(dir2Result);
        Assert.Contains("dir1/file1.txt", dir1Result);
        Assert.Contains("dir2/file2.txt", dir2Result);
    }

    [Fact]
    public async Task Cache_ShouldReturnCachedContent_OnSubsequentReads()
    {
        // Arrange
        string filePath = "cache-behavior-test.txt";
        string content = "Cached content";
        await _localProvider.WriteFileAsync(filePath, content);

        // Act: First read (cache miss)
        var firstRead = await _cachedProvider.ReadFileAsync(filePath);
        Assert.Equal(content, firstRead);

        // Modify file directly on filesystem (bypassing cache)
        string modifiedContent = "Modified content";
        await File.WriteAllTextAsync(Path.Combine(_tempBasePath, filePath), modifiedContent);

        // Act: Second read (should return cached content, not modified)
        var secondRead = await _cachedProvider.ReadFileAsync(filePath);

        // Assert: Should return cached content, not the modified file
        Assert.Equal(content, secondRead);
        Assert.NotEqual(modifiedContent, secondRead);
    }

    [Fact]
    public async Task Cache_ShouldBeCleared_AfterWriteOperation()
    {
        // Arrange
        string filePath = "cache-clear-test.txt";
        string initialContent = "Initial";
        string newContent = "New";

        await _localProvider.WriteFileAsync(filePath, initialContent);

        // Populate cache
        await _cachedProvider.ReadFileAsync(filePath);

        // Modify file directly
        await File.WriteAllTextAsync(Path.Combine(_tempBasePath, filePath), "Direct modification");

        // Act: Write through cached provider (should clear cache)
        await _cachedProvider.WriteFileAsync(filePath, newContent);

        // Assert: Next read should get the new content
        var result = await _cachedProvider.ReadFileAsync(filePath);
        Assert.Equal(newContent, result);
    }

    [Fact]
    public async Task Cache_ShouldHandleMultipleFiles_Independently()
    {
        // Arrange
        string file1 = "cache-file1.txt";
        string file2 = "cache-file2.txt";
        string content1 = "Content 1";
        string content2 = "Content 2";

        await _localProvider.WriteFileAsync(file1, content1);
        await _localProvider.WriteFileAsync(file2, content2);

        // Act: Read both files to populate cache
        var read1 = await _cachedProvider.ReadFileAsync(file1);
        var read2 = await _cachedProvider.ReadFileAsync(file2);

        // Assert: Both should be cached
        Assert.Equal(content1, read1);
        Assert.Equal(content2, read2);

        // Modify file2 directly
        await File.WriteAllTextAsync(Path.Combine(_tempBasePath, file2), "Modified 2");

        // Act: Write file1 (should only invalidate file1's cache)
        await _cachedProvider.WriteFileAsync(file1, "New 1");

        // Assert: file1 should have new content, file2 should still be cached (old content)
        var newRead1 = await _cachedProvider.ReadFileAsync(file1);
        var cachedRead2 = await _cachedProvider.ReadFileAsync(file2);

        Assert.Equal("New 1", newRead1);
        Assert.Equal(content2, cachedRead2); // Still cached, not the modified version
    }

    [Fact]
    public async Task Cache_ShouldHandleErrors_Gracefully()
    {
        // Arrange
        string filePath = "nonexistent.txt";

        // Act & Assert: Reading non-existent file should throw and not cache
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _cachedProvider.ReadFileAsync(filePath));

        // Verify cache doesn't contain the error
        // (If we try to read again, it should still throw, not return cached error)
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _cachedProvider.ReadFileAsync(filePath));
    }


    [Fact]
    public async Task FullWorkflow_ShouldWorkCorrectly_WithCaching()
    {
        // Arrange
        string filePath = "workflow-test.txt";
        string initialContent = "Initial";
        string updatedContent = "Updated";

        // Act: Write initial file
        await _cachedProvider.WriteFileAsync(filePath, initialContent);

        // Read (cache miss)
        var read1 = await _cachedProvider.ReadFileAsync(filePath);
        Assert.Equal(initialContent, read1);

        // Read again (cache hit)
        var read2 = await _cachedProvider.ReadFileAsync(filePath);
        Assert.Equal(initialContent, read2);

        // List files (cache miss)
        var list1 = await _cachedProvider.ListFilesAsync("", recursive: true);
        Assert.Contains(filePath, list1);

        // List files again (cache hit)
        var list2 = await _cachedProvider.ListFilesAsync("", recursive: true);
        Assert.Equal(list1.Count, list2.Count);

        // Update file (should invalidate cache)
        await _cachedProvider.WriteFileAsync(filePath, updatedContent);

        // Read updated file (should get fresh content)
        var read3 = await _cachedProvider.ReadFileAsync(filePath);
        Assert.Equal(updatedContent, read3);

        // Delete file
        await _cachedProvider.DeleteFileAsync(filePath);

        // Verify file is deleted
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _cachedProvider.ReadFileAsync(filePath));
    }
}

