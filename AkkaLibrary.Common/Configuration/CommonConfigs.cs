using System;
using System.Linq;
using Akka.Configuration;
using AkkaLibrary.Common.Interfaces;

namespace AkkaLibrary.Common.Configuration
{
    /// <summary>
    /// A set of common configurations that can be used to create more complex
    /// configurations with the desired fallback structure.
    /// </summary>
    public static class CommonConfigs
    {
        public static Config BasicConfig()
            => ConfigurationFactory
                .ParseString(
                    @"akka
                    {
                        suppress-json-serializer-warning = true
                        extensions = [""Akka.Cluster.Tools.PublishSubscribe.DistributedPubSubExtensionProvider,Akka.Cluster.Tools""],
                        actor
                        {
                            serializers
                            {
                                hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                            }
                            serialization-bindings
                            {
                                ""System.Object"" = hyperion
                            }
                        }
                    }");

        /// <summary>
        /// Creates a <see cref="Config"/> from an <see cref="ILoggingConfig"/> object
        /// </summary>
        /// <param name="cfg">The <see cref="ILoggingConfig"/></param>
        /// <returns><see cref="Config"/></returns>
        public static Config CreateLoggingConfig(ILoggingConfig cfg)
            => ConfigurationFactory
                .ParseString(
                    $"akka.loglevel = {cfg.LogLevel}, akka.loggers = [\"{cfg.LoggerClassName}, {cfg.LoggerBinaryName}\"]"
                    );
    }
}