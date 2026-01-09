using System.Net;
using System.Text;
using Google;
using Google.Apis.Drive.v3.Data;
using Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using System.IO;

namespace NexusFS.Tests
{
    public class GoogleDriveProviderTests
    {
        private readonly Mock<IGoogleDriveClient> _clientMock;
        private readonly GoogleDriveProvider _provider;

        public GoogleDriveProviderTests()
        {
            _clientMock = new Mock<IGoogleDriveClient>(MockBehavior.Strict);
            _provider = new GoogleDriveProvider("gdrive-test", _clientMock.Object, new MemoryCache(new MemoryCacheOptions()));

            _provider.Initialize(new Dictionary<string, string>
            {
                { "clientId", "dummy" },
                { "clientSecret", "dummy" },
                { "refreshToken", "dummy" }
            }).Wait();
        }

        [Fact]
        public async Task ReadFileAsync_ShouldDownloadUsingResolvedId()
        {
            _clientMock.Setup(c => c.FindItemAsync("root", "docs", GoogleDriveProvider.FolderMimeType))
                .ReturnsAsync(new DriveFile { Id = "folder-1", MimeType = GoogleDriveProvider.FolderMimeType });
            _clientMock.Setup(c => c.FindItemAsync("folder-1", "test.txt", null))
                .ReturnsAsync(new DriveFile { Id = "file-1", MimeType = "text/plain" });
            _clientMock.Setup(c => c.DownloadFileAsync("file-1"))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("hello drive")));

            var content = await _provider.ReadFileAsync("docs/test.txt");

            Assert.Equal("hello drive", content);
            _clientMock.VerifyAll();
        }

        [Fact]
        public async Task WriteFileAsync_ShouldCreateFolderAndUploadWhenMissing()
        {
            _clientMock.Setup(c => c.FindItemAsync("root", "docs", GoogleDriveProvider.FolderMimeType))
                .ReturnsAsync((DriveFile?)null);
            _clientMock.Setup(c => c.CreateFolderAsync("root", "docs"))
                .ReturnsAsync("folder-1");
            _clientMock.Setup(c => c.FindItemAsync("folder-1", "notes.txt", null))
                .ReturnsAsync((DriveFile?)null);
            _clientMock.Setup(c => c.UploadFileAsync("folder-1", "notes.txt", It.IsAny<Stream>()))
                .ReturnsAsync("file-1");

            await _provider.WriteFileAsync("docs/notes.txt", "content");

            _clientMock.Verify(c => c.CreateFolderAsync("root", "docs"), Times.Once);
            _clientMock.Verify(c => c.UploadFileAsync("folder-1", "notes.txt", It.IsAny<Stream>()), Times.Once);
        }

        [Fact]
        public async Task WriteFileAsync_ShouldUpdateExistingFile()
        {
            _clientMock.Setup(c => c.FindItemAsync("root", "docs", GoogleDriveProvider.FolderMimeType))
                .ReturnsAsync(new DriveFile { Id = "folder-1", MimeType = GoogleDriveProvider.FolderMimeType });
            _clientMock.Setup(c => c.FindItemAsync("folder-1", "notes.txt", null))
                .ReturnsAsync(new DriveFile { Id = "file-1", MimeType = "text/plain" });
            _clientMock.Setup(c => c.UpdateFileAsync("file-1", It.IsAny<Stream>()))
                .ReturnsAsync("file-1");

            await _provider.WriteFileAsync("docs/notes.txt", "updated");

            _clientMock.Verify(c => c.UpdateFileAsync("file-1", It.IsAny<Stream>()), Times.Once);
        }

        [Fact]
        public async Task ListFilesAsync_ShouldTraverseRecursively()
        {
            _clientMock.Setup(c => c.FindItemAsync("root", "docs", GoogleDriveProvider.FolderMimeType))
                .ReturnsAsync(new DriveFile { Id = "folder-1", MimeType = GoogleDriveProvider.FolderMimeType });

            _clientMock.Setup(c => c.ListChildrenAsync("folder-1"))
                .ReturnsAsync(new List<DriveFile>
                {
                    new DriveFile { Id = "sub-1", Name = "sub", MimeType = GoogleDriveProvider.FolderMimeType },
                    new DriveFile { Id = "file-1", Name = "root.txt", MimeType = "text/plain" }
                });

            _clientMock.Setup(c => c.ListChildrenAsync("sub-1"))
                .ReturnsAsync(new List<DriveFile>
                {
                    new DriveFile { Id = "file-2", Name = "deep.txt", MimeType = "text/plain" }
                });

            var files = await _provider.ListFilesAsync("docs", recursive: true);

            Assert.Contains("docs/root.txt", files);
            Assert.Contains("docs/sub/deep.txt", files);
            _clientMock.Verify(c => c.ListChildrenAsync("sub-1"), Times.Once);
        }

        [Fact]
        public async Task Operations_ShouldRetryOnRateLimit()
        {
            var exception = new GoogleApiException("drive", "rate limit")
            {
                HttpStatusCode = HttpStatusCode.TooManyRequests
            };

            _clientMock.SetupSequence(c => c.FindItemAsync("root", "docs", GoogleDriveProvider.FolderMimeType))
                .ThrowsAsync(exception)
                .ReturnsAsync(new DriveFile { Id = "folder-1", MimeType = GoogleDriveProvider.FolderMimeType });

            _clientMock.Setup(c => c.FindItemAsync("folder-1", "test.txt", null))
                .ReturnsAsync(new DriveFile { Id = "file-1", MimeType = "text/plain" });
            _clientMock.Setup(c => c.DownloadFileAsync("file-1"))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("retry-ok")));

            var content = await _provider.ReadFileAsync("docs/test.txt");

            Assert.Equal("retry-ok", content);
            _clientMock.Verify(c => c.FindItemAsync("root", "docs", GoogleDriveProvider.FolderMimeType), Times.Exactly(2));
        }
    }
}

