using System.Collections.Generic;
using System.Linq;
using AkkaLibrary.Cluster.Interfaces;

namespace AkkaLibrary.Cluster.Configuration
{
    /// <summary>
    /// 
    /// </summary>
    public class DefaultClusterConfig : IClusterConfig
    {
        /// <summary>
        /// Creates a default instance of <see cref="DefaultClusterConfig"/>
        /// </summary>
        public DefaultClusterConfig() { }

        /// <summary>
        /// Creates an instance of <see cref="DefaultClusterConfig"/>
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public DefaultClusterConfig(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
        }

        /// <summary>
        /// Creates an instance of <see cref="DefaultClusterConfig"/>
        /// </summary>
        /// <param name="hostname">The hostname</param>
        /// <param name="port">The port number</param>
        /// <param name="minNodes">Minimum nodes required in the cluster</param>
        /// <param name="seedNodes">Full URIs of seed nodes</param>
        /// <param name="roles">Roles and the minimum number of those roles required in the cluster</param>
        /// <returns></returns>
        public DefaultClusterConfig(
            string hostname,
            int port,
            int minNodes = 1,
            IEnumerable<string> seedNodes = null,
            IEnumerable<KeyValuePair<string, int>> roles = null,
            bool enablePubSub = true
            ) : this(hostname, port)
        {
            MinNodeNumberForUp = minNodes;
            SeedNodePaths = seedNodes?.ToArray() ?? new string[] { };
            Roles = roles?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, int>();
            WithPubSub = enablePubSub;
        }

        /// <inheritdoc/>
        public int MinNodeNumberForUp { get; set; } = 1;

        /// <inheritdoc/>
        public string[] SeedNodePaths { get; set; } = new[] { "akka.tcp://" };

        /// <inheritdoc/>
        public Dictionary<string, int> Roles { get; set; } = new Dictionary<string, int>();

        /// <inheritdoc/>
        public string Provider => "cluster";

        /// <inheritdoc/>
        public int Port { get; } = -1;

        /// <inheritdoc/>
        public string Hostname { get; } = "localhost";

        /// <inheritdoc/>
        public bool WithPubSub { get; } = false;
    }
}