using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Infrastructure.Services;
using Infrastructure.Services.Observability;
using Moq;
using Xunit;

namespace NexusFS.Tests
{
    public class ProviderFactoryTests
    {
        [Fact]
        public void CreateProvider_KnownTypes_ReturnsProviderInstances()
        {
            var factory = new ProviderFactory();

            var mem = factory.CreateProvider("memory", "mem-1");
            mem.Should().NotBeNull();
            mem.ProviderType.Should().Be("Memory");

            var local = factory.CreateProvider("local", "local-1");
            local.Should().NotBeNull();
            local.ProviderType.Should().StartWith("Local");
        }

        [Fact]
        public async Task CreateProviderAsync_WithConfiguration_InitializesProvider()
        {
            var factory = new ProviderFactory();
            var config = new Dictionary<string, string> {{"basePath", "/tmp"}};

            var p = await factory.CreateProviderAsync("memory", "mem-async", config);
            p.Should().NotBeNull();
            p.Configuration.Should().ContainKey("basePath");
        }

        [Fact]
        public void CreateProvider_Unsupported_Throws()
        {
            var factory = new ProviderFactory();
            Action act = () => factory.CreateProvider("unknown-type", "id");
            act.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void IsProviderTypeSupported_Works()
        {
            var factory = new ProviderFactory();
            factory.IsProviderTypeSupported("memory").Should().BeTrue();
            factory.IsProviderTypeSupported("nope").Should().BeFalse();
            factory.GetSupportedProviderTypes().Should().Contain("Memory");
        }
    }
}
