using System.Linq;
using Akka.Configuration;
using AkkaLibrary.Cluster.Interfaces;

namespace AkkaLibrary.Cluster.Configuration
{
    /// <summary>
    /// A set of common configurations that can be used to create more complex
    /// configurations with the desired fallback structure.
    /// </summary>
    public static class ClusterConfigs
    {
        /// <summary>
        /// Creates a <see cref="Config"/> from an <see cref="IRemotingConfig"/> object
        /// </summary>
        /// <param name="cfg">The <see cref="IRemotingConfig"/></param>
        /// <returns><see cref="Config"/></returns>
        public static Config CreateRemotingConfig(IRemotingConfig cfg)
            => ConfigurationFactory
                .ParseString($@"akka
                {{
                    actor
                    {{
                        provider = {cfg.Provider}
                    }}
                    
                    remote
                    {{
                        startup-timeout = 10s
                        shutdown-timeout = 10s
                        flush-wait-on-shutdown = 2s
                        use-passive-connections = on
                        
                        log-remote-lifecycle-events = on
                        
                        dot-netty.tcp
                        {{
                            port = {cfg.Port}
                            hostname = {cfg.Hostname}
                            transport-protocol = tcp
                            byte-order = ""little-endian""
                        }}
                    }}");

        /// <summary>
        /// Creates a <see cref="Config"/> from an <see cref="IClusterConfig"/> object.
        /// Initialises the necessary remoting properties necessary for clustering as well.
        /// </summary>
        /// <param name="cfg">The <see cref="IClusterConfig"/></param>
        /// <returns><see cref="Config"/></returns>
        public static Config CreateClusterConfig(IClusterConfig cfg)
        {
            var seedNodes = string.Join(",", cfg.SeedNodePaths.Select(p => $"\"{p}\""));

            var roles = string.Join(",", cfg.Roles.Keys);

            var roleNumbers = string.Join(",", cfg.Roles.Select(kvp => $"{kvp.Key}.min-nr-of-members = {kvp.Value}"));

            var mainConfig = ConfigurationFactory
                .ParseString(
                    $@"
                    akka.cluster
                    {{
                        min-nr-of-members = {cfg.MinNodeNumberForUp},
                        seed-nodes = [{seedNodes}],
                        roles = [{roles}],
                        role
                        {{
                            {roleNumbers}
                        }}
                    }}")
                    .WithFallback(CreateRemotingConfig(cfg));

            return mainConfig;
        }
    }
}