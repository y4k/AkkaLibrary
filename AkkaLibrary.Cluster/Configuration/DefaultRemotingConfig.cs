using AkkaLibrary.Cluster.Interfaces;

namespace AkkaLibrary.Cluster.Configuration
{
    /// <summary>
    /// Default configuration for remoting
    /// The hostname is "localhost" but the port defaults to -1 as
    /// this must be set
    /// </summary>
    /// <inheritdoc/>
    public class DefaultRemotingConfig : IRemotingConfig
    {
        /// <summary>
        /// Creates an instance of <see cref="DefaultRemotingConfig"/>
        /// </summary>
        public DefaultRemotingConfig() { }

        /// <summary>
        /// Creates an instance of <see cref="DefaultRemotingConfig"/>
        /// </summary>
        /// <param name="hostname">The hostname</param>
        /// <param name="port">The port number</param>
        public DefaultRemotingConfig(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
        }

        /// <summary>
        /// Fixed to "remote" as it will use the default remote actor ref provider
        /// </summary>
        /// <inheritdoc/>
        public string Provider { get; set; } = "remote";

        /// <inheritdoc/>
        public int Port { get; set; } = -1;

        /// <inheritdoc/>
        public string Hostname { get; set; } = "localhost";
    }
}