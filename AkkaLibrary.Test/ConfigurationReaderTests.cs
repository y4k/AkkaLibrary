using Akka.Configuration;
using AkkaLibrary.Configuration;
using FluentAssertions;
using Xunit;

namespace AkkaLibrary.Test
{
    public class ConfigurationReaderTests
    {
        [Fact(Skip = "Does not work currently")]
        public void CorrectlyAcquireAkkaSection()
        {
            var reader = new ConfigurationReader();

            var cfg = reader.GetAkkaHocon();

            cfg.Should().NotBeNull();

            cfg.GetConfig("akka.remote").Should().NotBeNull();

            cfg.GetInt("akka.remote.dot-netty.tcp.port").Should().Be(8080);
        }
    }
}