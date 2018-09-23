using Akka.Configuration;
using AkkaLibrary.Cluster.Configuration;

namespace AkkaLibrary.Cluster.Configuration
{
    /// <summary>
    /// Configuration constants for a cluster node
    /// </summary>
    public static class ClusterNodeConstants
    {
        /// <summary>
        /// HOCON location of the name of the type that creates actor props for the node
        /// </summary>
        public readonly static string HoconFactoryNode = "akka.actor.node-actor-factory";
        
        /// <summary>
        /// Default actor props factory class
        /// </summary>
        /// <returns></returns>
        public readonly static string DefaultFactoryType = typeof(DefaultClusterPropsFactory).FullName;

        /// <summary>
        /// Default <see cref="Config"/> combining the above
        /// </summary>
        /// <returns></returns>
        public static Config DefaultConfig()
            => ConfigurationFactory.ParseString($"{HoconFactoryNode} = {DefaultFactoryType}");

        /// <summary>
        /// Default actor system name
        /// </summary>
        public static readonly string DefaultSystemName = "actor-system";
    }
}