using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using AkkaLibrary.Common.Configuration;
using Moq;
using AkkaLibrary.Cluster.Interfaces;
using AkkaLibrary.Cluster.Configuration;
using AkkaLibrary.Common.Interfaces;

namespace AkkaLibrary.Test
{
    public class ConfigurationFactoryTests
    {
        [Fact]
        public void BasicConfigTest()
        {
            var cfg = CommonConfigs.BasicConfig();

            cfg.GetBoolean("akka.suppress-json-serializer-warning").Should().Be(true);
        }

        [Fact]
        public void RemotingConfiguration()
        {
            var hostname = "my-host";
            var port = 1234;
            var provider = "remote";

            var mock = new Mock<IRemotingConfig>();

            mock.Setup(x => x.Hostname).Returns(hostname);
            mock.Setup(x => x.Port).Returns(port);
            mock.Setup(x => x.Provider).Returns(provider);

            var cfg = ClusterConfigs.CreateRemotingConfig(mock.Object);

            cfg.GetString("akka.actor.provider").Should().Be(provider);
            cfg.GetString("akka.remote.dot-netty.tcp.hostname").Should().Be(hostname);
            cfg.GetInt("akka.remote.dot-netty.tcp.port").Should().Be(port);
        }

        [Fact]
        public void ClusterConfiguration()
        {
            var hostname = "my-host";
            var port = 1234;
            var provider = "cluster";
            var systemName = "System";
            var seedNodes = new[]{$"akka.tcp://{systemName}@{hostname}:{port}"};

            var roles = new Dictionary<string, int>
            {
                {"Worker", 1},
                {"Server", 2}
            };

            var mock = new Mock<IClusterConfig>();

            mock.Setup(x => x.Hostname).Returns(hostname);
            mock.Setup(x => x.Port).Returns(port);
            mock.Setup(x => x.Provider).Returns(provider);

            mock.Setup(x => x.SeedNodePaths).Returns(seedNodes);
            mock.Setup(x => x.MinNodeNumberForUp).Returns(1);
            mock.Setup(x => x.Roles).Returns(roles);

            var cfg = ClusterConfigs.CreateClusterConfig(mock.Object);

            cfg.GetString("akka.actor.provider").Should().Be(provider);
            cfg.GetString("akka.remote.dot-netty.tcp.hostname").Should().Be(hostname);
            cfg.GetInt("akka.remote.dot-netty.tcp.port").Should().Be(port);

            cfg.GetInt("akka.cluster.min-nr-of-members").Should().Be(1);
            cfg.GetStringList("akka.cluster.seed-nodes").Should().BeEquivalentTo(seedNodes);
            cfg.GetStringList("akka.cluster.roles").Should().BeEquivalentTo(roles.Keys);
            cfg.GetInt("akka.cluster.role.Worker.min-nr-of-members").Should().Be(1);
            cfg.GetInt("akka.cluster.role.Server.min-nr-of-members").Should().Be(2);
        }

        [Fact]
        public void LoggingConfiguration()
        {
            var level = LogLevelEnum.WARNING;
            var loggerName = "Name";
            var loggerBinary = "Binary";

            var mock = new Mock<ILoggingConfig>();

            mock.Setup(x => x.LogLevel).Returns(level);
            mock.Setup(x => x.LoggerClassName).Returns(loggerName);
            mock.Setup(x => x.LoggerBinaryName).Returns(loggerBinary);

            var cfg = CommonConfigs.CreateLoggingConfig(mock.Object);

            cfg.GetString("akka.loglevel").Should().Be(level.ToString());
            cfg.GetStringList("akka.loggers").Should().BeEquivalentTo(new[]{$"{loggerName}, {loggerBinary}"});
        }
    }
}
