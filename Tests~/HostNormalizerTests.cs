using System;
using AppDiscovery.Internal;
using Xunit;

namespace AppDiscovery.Tests
{
    public class HostNormalizerTests
    {
        [Fact]
        public void AcceptsAPlainHostAndLowerCasesIt()
        {
            Assert.Equal("offers.example.com", HostNormalizer.Normalize("Offers.Example.com"));
        }

        [Theory]
        [InlineData("https://offers.example.com/")]
        [InlineData("HTTPS://offers.example.com//")]
        [InlineData("  offers.example.com  ")]
        public void AcceptsAnHttpsUrlTrailingSlashesAndWhitespace(string raw)
        {
            Assert.Equal("offers.example.com", HostNormalizer.Normalize(raw));
        }

        [Fact]
        public void AcceptsAPort()
        {
            Assert.Equal("offers.example.com:8443", HostNormalizer.Normalize("offers.example.com:8443"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RejectsBlankValues(string raw)
        {
            var ex = Assert.Throws<ArgumentException>(() => HostNormalizer.Normalize(raw));
            Assert.Contains("host is required", ex.Message);
        }

        [Theory]
        [InlineData("http://offers.example.com")]
        [InlineData("ftp://offers.example.com")]
        public void RejectsCleartextHttpAndOtherSchemes(string raw)
        {
            Assert.Throws<ArgumentException>(() => HostNormalizer.Normalize(raw));
        }

        [Theory]
        [InlineData("offers.example.com/path")]
        [InlineData("offers.example.com?x=1")]
        [InlineData("offers example.com")]
        [InlineData("-offers.example.com")]
        public void RejectsPathsQueriesAndWhitespace(string raw)
        {
            Assert.Throws<ArgumentException>(() => HostNormalizer.Normalize(raw));
        }

        [Fact]
        public void NamesTheSettingInTheError()
        {
            var ex = Assert.Throws<ArgumentException>(() => HostNormalizer.Normalize("", "trackerHost"));
            Assert.Equal("trackerHost", ex.ParamName);
        }

        [Fact]
        public void AnOptionalHostMayBeBlankButNeverInvalid()
        {
            Assert.Null(HostNormalizer.NormalizeOptional(null, "trackerHost"));
            Assert.Null(HostNormalizer.NormalizeOptional("  ", "trackerHost"));
            Assert.Equal("track.example.com", HostNormalizer.NormalizeOptional("Track.Example.com", "trackerHost"));
            Assert.Throws<ArgumentException>(() => HostNormalizer.NormalizeOptional("a b", "trackerHost"));
        }
    }
}
