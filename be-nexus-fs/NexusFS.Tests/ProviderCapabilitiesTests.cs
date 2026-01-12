using Domain.Models;
using FluentAssertions;
using Xunit;

namespace NexusFS.Tests
{
    public class ProviderCapabilitiesTests
    {
        [Fact]
        public void Default_ShouldReturnBasicCapabilities()
        {
            // Act
            var capabilities = ProviderCapabilities.Default();

            // Assert
            capabilities.Should().NotBeNull();
            capabilities.SupportsDirectories.Should().BeTrue();
            capabilities.SupportsRecursiveOperations.Should().BeTrue();
            capabilities.SupportsStreaming.Should().BeTrue();
            capabilities.SupportsMetadata.Should().BeTrue();
            capabilities.SupportsAtomicRename.Should().BeFalse();
            capabilities.SupportsVersioning.Should().BeFalse();
        }

        [Fact]
        public void ForLocal_ShouldReturnLocalFileSystemCapabilities()
        {
            // Act
            var capabilities = ProviderCapabilities.ForLocal();

            // Assert
            capabilities.Should().NotBeNull();
            capabilities.SupportsAtomicRename.Should().BeTrue();
            capabilities.SupportsSymlinks.Should().BeTrue();
            capabilities.SupportsHardLinks.Should().BeTrue();
            capabilities.SupportsFileLocking.Should().BeTrue();
            capabilities.SupportsServerSideCopy.Should().BeTrue();
            capabilities.SupportsServerSideMove.Should().BeTrue();
            capabilities.SupportsAcls.Should().BeTrue();
        }

        [Fact]
        public void ForS3_ShouldReturnS3Capabilities()
        {
            // Act
            var capabilities = ProviderCapabilities.ForS3();

            // Assert
            capabilities.Should().NotBeNull();
            capabilities.SupportsDirectories.Should().BeFalse(); // S3 uses key prefixes
            capabilities.SupportsVersioning.Should().BeTrue();
            capabilities.SupportsPartialReads.Should().BeTrue();
            capabilities.SupportsBatchOperations.Should().BeTrue();
            capabilities.SupportsEncryption.Should().BeTrue();
            capabilities.SupportsAcls.Should().BeTrue();
            capabilities.SupportsServerSideCopy.Should().BeTrue();
            capabilities.MaxFileSize.Should().Be(5L * 1024 * 1024 * 1024 * 1024); // 5 TB
        }

        [Fact]
        public void ForGoogleDrive_ShouldReturnGoogleDriveCapabilities()
        {
            // Act
            var capabilities = ProviderCapabilities.ForGoogleDrive();

            // Assert
            capabilities.Should().NotBeNull();
            capabilities.SupportsDirectories.Should().BeTrue();
            capabilities.SupportsVersioning.Should().BeTrue();
            capabilities.SupportsSearch.Should().BeTrue();
            capabilities.SupportsBatchOperations.Should().BeTrue();
            capabilities.SupportsEncryption.Should().BeTrue();
            capabilities.SupportsAcls.Should().BeTrue();
            capabilities.SupportsServerSideCopy.Should().BeTrue();
            capabilities.SupportsServerSideMove.Should().BeTrue();
            capabilities.MaxFileSize.Should().Be(5L * 1024 * 1024 * 1024 * 1024); // 5 TB
        }

        [Fact]
        public void ForFtp_ShouldReturnFtpCapabilities()
        {
            // Act
            var capabilities = ProviderCapabilities.ForFtp();

            // Assert
            capabilities.Should().NotBeNull();
            capabilities.SupportsAtomicRename.Should().BeTrue();
            capabilities.SupportsDirectories.Should().BeTrue();
            capabilities.SupportsServerSideMove.Should().BeTrue();
            capabilities.SupportsVersioning.Should().BeFalse();
            capabilities.SupportsBatchOperations.Should().BeFalse();
            capabilities.SupportsEncryption.Should().BeFalse();
        }

        [Fact]
        public void ForWebDAV_ShouldReturnWebDAVCapabilities()
        {
            // Act
            var capabilities = ProviderCapabilities.ForWebDAV();

            // Assert
            capabilities.Should().NotBeNull();
            capabilities.SupportsDirectories.Should().BeTrue();
            capabilities.SupportsFileLocking.Should().BeTrue();
            capabilities.SupportsPartialReads.Should().BeTrue();
            capabilities.SupportsServerSideCopy.Should().BeTrue();
            capabilities.SupportsServerSideMove.Should().BeTrue();
            capabilities.SupportsAcls.Should().BeTrue();
        }

        [Fact]
        public void CustomCapabilities_ShouldBeSettable()
        {
            // Arrange
            var capabilities = ProviderCapabilities.Default();
            var customData = new Dictionary<string, object>
            {
                { "customFeature1", true },
                { "maxConnections", 100 }
            };

            // Act
            capabilities.CustomCapabilities = customData;

            // Assert
            capabilities.CustomCapabilities.Should().NotBeNull();
            capabilities.CustomCapabilities.Should().ContainKeys("customFeature1", "maxConnections");
            capabilities.CustomCapabilities["customFeature1"].Should().Be(true);
            capabilities.CustomCapabilities["maxConnections"].Should().Be(100);
        }
    }
}

