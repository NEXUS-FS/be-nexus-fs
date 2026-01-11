using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Infrastructure.Services;
using Xunit;

namespace NexusFS.Tests
{
    public class MemoryProviderStreamTests
    {
        [Fact]
        public async Task WriteStreamAndReadStream_Work()
        {
            var provider = new MemoryProvider("mem-stream");

            var bytes = Encoding.UTF8.GetBytes("stream-data");
            using (var ms = new MemoryStream(bytes))
            {
                await provider.WriteStreamAsync("folder/stream.txt", ms);
            }

            using var readStream = await provider.ReadStreamAsync("folder/stream.txt");
            using var sr = new StreamReader(readStream, Encoding.UTF8);
            var text = await sr.ReadToEndAsync();
            text.Should().Be("stream-data");
        }

        [Fact]
        public async Task StatAndExists_AfterWrite_ReturnsTrueAndSize()
        {
            var provider = new MemoryProvider("mem-stat");
            await provider.WriteFileAsync("a.bin", "abc");

            var exists = await provider.ExistsAsync("a.bin");
            exists.Should().BeTrue();

            var meta = await provider.StatAsync("a.bin");
            meta.Exists.Should().BeTrue();
            meta.Size.Should().Be(3);
        }

        [Fact]
        public async Task ReadFile_NonExisting_Throws()
        {
            var provider = new MemoryProvider("mem-stat-2");
            await Assert.ThrowsAsync<System.IO.FileNotFoundException>(async () => await provider.ReadFileAsync("nope.txt"));
        }
    }
}
