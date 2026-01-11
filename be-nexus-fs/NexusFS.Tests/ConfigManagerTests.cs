using System.Collections.Generic;
using Application.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace NexusFS.Tests
{
    public class ConfigManagerTests
    {
        private IConfiguration BuildConfig(Dictionary<string, string?> data) =>
            new ConfigurationBuilder().AddInMemoryCollection(data).Build();

        [Fact]
        public void GetConfigValue_ReturnsValue_And_UsesCacheOverride()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                {"App:Retries", "3"}
            });
            var manager = new ConfigManager(config);

            var v1 = manager.GetConfigValue<int>("App:Retries");
            v1.Should().Be(3);

            manager.SetConfiguration("App:Retries", 10);
            var v2 = manager.GetConfigValue<int>("App:Retries");
            v2.Should().Be(10);
        }

        private class JwtOptions
        {
            public string Issuer { get; set; } = string.Empty;
            public string Audience { get; set; } = string.Empty;
        }

        [Fact]
        public void BindSection_BindsAndCachesTypedSection()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                {"Jwt:Issuer", "nexus"},
                {"Jwt:Audience", "fs"}
            });
            var manager = new ConfigManager(config);

            var s1 = manager.BindSection<JwtOptions>("Jwt");
            var s2 = manager.BindSection<JwtOptions>("Jwt");

            s1.Issuer.Should().Be("nexus");
            s1.Audience.Should().Be("fs");
            ReferenceEquals(s1, s2).Should().BeTrue();
        }

        [Fact]
        public void GetConnectionString_ReturnsConfiguredString()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                {"ConnectionStrings:Default", "Host=localhost;Port=5432"}
            });
            var manager = new ConfigManager(config);

            manager.GetConnectionString("Default").Should().Be("Host=localhost;Port=5432");
        }

        [Fact]
        public void ClearCache_RemovesOverrides_AndReadsUnderlyingConfiguration()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                {"Feature:Enabled", "true"}
            });
            var manager = new ConfigManager(config);

            manager.SetConfiguration("Feature:Enabled", false);
            manager.GetConfigValue<bool>("Feature:Enabled").Should().BeFalse();

            manager.ClearCache();
            manager.GetConfigValue<bool>("Feature:Enabled").Should().BeTrue();
        }

        [Fact]
        public void GetConfigValue_WithDefault_FallsBackOnMissingOrError()
        {
            var config = BuildConfig(new Dictionary<string, string?>());
            var manager = new ConfigManager(config);

            // For value types, IConfiguration returns default(T) when missing (e.g., 0 for int)
            manager.GetConfigValue("Missing:Int", 42).Should().Be(0);
            manager.GetConfigValue("Missing:String", "x").Should().Be("x");
        }
    }
}
