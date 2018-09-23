using Akka.Configuration;

namespace AkkaLibrary.Cluster.Configuration
{
    /// <summary>
    /// Simple router configuration factories
    /// </summary>
    public static class RouterConfigurationFactories
    {
        /// <summary>
        /// Group router that when used will send messages to all
        /// actors with the path "Configurator" on all nodes with
        /// the use-role "satellite"
        /// </summary>
        /// <returns></returns>
        public static Config CreateConfiguratorGroupRouterConfig()
            => ConfigurationFactory.ParseString(
                $@"
                akka
                {{
                    actor
                    {{
                        deployment
                        {{
                            /configurator/broadcaster
                            {{
                                router = broadcast-group #Routing strategy
                                routees.paths = [""configurator""]
                                use-role = satellite
                                cluster
                                {{
                                    enabled = on
                                    allow-local-routees = off
                                }}
                            }}
                        }}
                    }}
                }}
                ");
    }
}