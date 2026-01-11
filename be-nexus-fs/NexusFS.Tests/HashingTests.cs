using System;
using System.Text;
using FluentAssertions;
using Infrastructure.Cache.Hashing;
using Xunit;

namespace NexusFS.Tests
{
    public class HashingTests
    {
        [Fact]
        public void FNV1a_Hash32_ByteArray_ReturnsExpectedHash()
        {
            // Arrange
            byte[] data = Encoding.UTF8.GetBytes("test");

            // Act
            uint hash = FNV1a.Hash32(data);

            // Assert - Using the actual computed value
            hash.Should().Be(2949673445U);
        }

        [Fact]
        public void FNV1a_Hash32_String_ReturnsExpectedHash()
        {
            // Arrange
            string input = "test";

            // Act
            uint hash = FNV1a.Hash32(input);

            // Assert
            hash.Should().Be(2949673445); // Same as byte array version
        }

        [Fact]
        public void FNV1a_Hash32_NullByteArray_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => FNV1a.Hash32((byte[])null));
        }

        [Fact]
        public void FNV1a_Hash32_NullString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => FNV1a.Hash32((string)null));
        }

        [Fact]
        public void FNV1a_Hash64_ByteArray_ReturnsExpectedHash()
        {
            // Arrange
            byte[] data = Encoding.UTF8.GetBytes("test");

            // Act
            ulong hash = FNV1a.Hash64(data);

            // Assert
            hash.Should().Be(18007334074686647077UL);
        }

        [Fact]
        public void FNV1a_Hash64_String_ReturnsExpectedHash()
        {
            // Arrange
            string input = "test";

            // Act
            ulong hash = FNV1a.Hash64(input);

            // Assert
            hash.Should().Be(18007334074686647077UL); // Same as byte array version
        }

        [Fact]
        public void FNV1a_Hash64_NullByteArray_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => FNV1a.Hash64((byte[])null));
        }

        [Fact]
        public void FNV1a_Hash64_NullString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => FNV1a.Hash64((string)null));
        }

        [Fact]
        public void FNV1a32_Constructor_InitializesWithOffsetBasis()
        {
            // Act
            var hasher = new FNV1a32();

            // Assert
            hasher.Digest().Should().Be(2166136261U); // FNV_OFFSET_BASIS_32
        }

        [Fact]
        public void FNV1a32_Reset_SetsHashToOffsetBasis()
        {
            // Arrange
            var hasher = new FNV1a32();
            hasher.Update("test");

            // Act
            hasher.Reset();

            // Assert
            hasher.Digest().Should().Be(2166136261U); // FNV_OFFSET_BASIS_32
        }

        [Fact]
        public void FNV1a32_Update_ByteArray_UpdatesHash()
        {
            // Arrange
            var hasher = new FNV1a32();
            byte[] data = Encoding.UTF8.GetBytes("test");

            // Act
            hasher.Update(data);

            // Assert
            hasher.Digest().Should().Be(2949673445U);
        }

        [Fact]
        public void FNV1a32_Update_String_UpdatesHash()
        {
            // Arrange
            var hasher = new FNV1a32();

            // Act
            hasher.Update("test");

            // Assert
            hasher.Digest().Should().Be(2949673445U);
        }

        [Fact]
        public void FNV1a32_Update_Byte_UpdatesHash()
        {
            // Arrange
            var hasher = new FNV1a32();

            // Act
            hasher.Update((byte)'t');

            // Assert (actual value from implementation)
            hasher.Digest().Should().Be(4044111267U);
        }

        [Fact]
        public void FNV1a32_Update_NullByteArray_ThrowsArgumentNullException()
        {
            // Arrange
            var hasher = new FNV1a32();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => hasher.Update((byte[])null));
        }

        [Fact]
        public void FNV1a32_Update_NullString_ThrowsArgumentNullException()
        {
            // Arrange
            var hasher = new FNV1a32();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => hasher.Update((string)null));
        }

        [Fact]
        public void FNV1a32_IncrementalUpdate_ProducesCorrectHash()
        {
            // Arrange
            var hasher = new FNV1a32();

            // Act
            hasher.Update("te");
            hasher.Update("st");

            // Assert
            hasher.Digest().Should().Be(2949673445U); // Same as "test"
        }

        [Fact]
        public void FNV1a64_Constructor_InitializesWithOffsetBasis()
        {
            // Act
            var hasher = new FNV1a64();

            // Assert
            hasher.Digest().Should().Be(14695981039346656037UL); // FNV_OFFSET_BASIS_64
        }

        [Fact]
        public void FNV1a64_Reset_SetsHashToOffsetBasis()
        {
            // Arrange
            var hasher = new FNV1a64();
            hasher.Update("test");

            // Act
            hasher.Reset();

            // Assert
            hasher.Digest().Should().Be(14695981039346656037UL); // FNV_OFFSET_BASIS_64
        }

        [Fact]
        public void FNV1a64_Update_ByteArray_UpdatesHash()
        {
            // Arrange
            var hasher = new FNV1a64();
            byte[] data = Encoding.UTF8.GetBytes("test");

            // Act
            hasher.Update(data);

            // Assert
            hasher.Digest().Should().Be(18007334074686647077UL);
        }

        [Fact]
        public void FNV1a64_Update_String_UpdatesHash()
        {
            // Arrange
            var hasher = new FNV1a64();

            // Act
            hasher.Update("test");

            // Assert
            hasher.Digest().Should().Be(18007334074686647077UL);
        }

        [Fact]
        public void FNV1a64_Update_Byte_UpdatesHash()
        {
            // Arrange
            var hasher = new FNV1a64();

            // Act
            hasher.Update((byte)'t');

            // Assert (actual value from implementation)
            hasher.Digest().Should().Be(12638201494206808739UL);
        }

        [Fact]
        public void FNV1a64_Update_NullByteArray_ThrowsArgumentNullException()
        {
            // Arrange
            var hasher = new FNV1a64();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => hasher.Update((byte[])null));
        }

        [Fact]
        public void FNV1a64_Update_NullString_ThrowsArgumentNullException()
        {
            // Arrange
            var hasher = new FNV1a64();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => hasher.Update((string)null));
        }

        [Fact]
        public void FNV1a64_IncrementalUpdate_ProducesCorrectHash()
        {
            // Arrange
            var hasher = new FNV1a64();

            // Act
            hasher.Update("te");
            hasher.Update("st");

            // Assert
            hasher.Digest().Should().Be(18007334074686647077UL); // Same as "test"
        }
    }
}