using System;
using System.IO;
using System.Linq;
using Akka.Actor;
using Akka.Cluster.Routing;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Serilog;

namespace AkkaLibrary.Cluster.Hub
{
    /// <summary>
    /// Actor system factory that produces the empty actor system used by the Hub.
    /// 
    /// Requires the "hub.hocon" file
    /// </summary>
    public static class HubSystemFactory
    {
        /// <summary>
        /// Creates a new actor system according to the supplied parameters and the
        /// "hub.hocon" file
        /// 
        /// Assumed transport used is tcp
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <param name="hostname">IP address or hostname</param>
        /// <param name="requestedPort">Non-zero port</param>
        /// <returns>The Actor System</returns>
        public static ActorSystem Create(string systemName, string hostname, int? requestedPort)
        {
            if(!File.Exists("hub.hocon"))
            {
                throw new FileLoadException($"Could not find 'hub.hocon' file in {Directory.GetCurrentDirectory()}");
            }

            // Reads in the entire hocon configuration and parses to a Config object
            var configText = File.ReadAllText("hub.hocon");
            var clusterConfig = ConfigurationFactory.ParseString(configText);

            // Find the "hub" specific configuration
            var hubConfig = clusterConfig.GetConfig("hub");
            // Set the actor system name if supplied value is null or empty
            if(hubConfig != null && string.IsNullOrEmpty(systemName))
            {
                systemName = hubConfig.GetString("actorsystem");
            }

            var remoteConfig = clusterConfig.GetConfig("akka.remote");
            if(string.IsNullOrEmpty(hostname))
            {
                // Defaults to localhost as final step
                hostname = remoteConfig.GetString("dot-netty.tcp.public-hostname") ?? "127.0.0.1";
            }

            int port = requestedPort ?? remoteConfig.GetInt("dot-netty.tcp.port");
            if(port == 0)
            {
                throw new ConfigurationException("An explicit port must be specified for the Hub. Zero is unnacceptable.");
            }

            // Display configuration
            Log.Information($"[Cluster Hub] ActorSystem: {systemName}; IP: {hostname}; PORT: {port}");
            Log.Information($"[Cluster Hub] Parsing address");
            Log.Information("[Cluster Hub] Parse successful");
            Log.Information("[Cluster Hub] Public hostname {PublicHubHostname}", hostname);

            var selfAddress = new Address("akka.tcp", systemName, hostname.Trim(), port).ToString();            

            var seeds = clusterConfig.GetStringList("akka.cluster.seed-nodes");
            seeds.Add(selfAddress);

            var injectedClusterConfigString = seeds.Aggregate("akka.cluster.seed-nodes = [", (current, seed) => current + (@"""" + seed + @""","));
            injectedClusterConfigString += "]";

            var finalConfig = ConfigurationFactory.ParseString(
                string.Format("akka.remote.dot-netty.tcp.public-hostname = {0}, akka.remote.dot-netty.tcp.port = {1}", hostname, port)
                )
                .WithFallback(ConfigurationFactory.ParseString(injectedClusterConfigString))
                .WithFallback(clusterConfig);

            // Create the actor system
            var system = ActorSystem.Create(systemName, finalConfig);

            var proxySettings = ClusterSingletonProxySettings.Create(system).WithBufferSize(1000);

            var name = "manager";

            // Create the cluster singleton proxy for the node manager
            var managerProxy = system.ActorOf(
                ClusterSingletonProxy.Props(
                    singletonManagerPath:$"/user/{name}",
                    settings: proxySettings
                    ),
                    $"{name}-proxy");

            // Subscribe the forwarder to the client receptionist topic
            var forwarder = system.ActorOf(Props.Create(() => new PublisherActor("client-messages")), "forwarder");
            ClusterClientReceptionist.Get(system).RegisterSubscriber("client-messages", forwarder);
            // ClusterClientReceptionist.Get(system).RegisterService(managerProxy);

            return system;
        }
    }
}