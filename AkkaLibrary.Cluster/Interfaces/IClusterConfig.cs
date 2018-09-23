using System.Collections.Generic;

namespace AkkaLibrary.Cluster.Interfaces
{
    /// <summary>
    /// Interface for specifying clustering and subsequently remoting criteria
    /// in an Akka.NET <see cref="Config"/>
    /// </summary>
    public interface IClusterConfig : IRemotingConfig
    {
        /// <summary>
        /// The minimum number of nodes required for the cluster to be
        /// considered up
        /// </summary>
        /// <returns>Minimum Nodes</returns>
        int MinNodeNumberForUp { get; }

        /// <summary>
        /// Full URIs in the form of "akka.tcp://system-name@hostname:port"
        /// </summary>
        /// <returns></returns>
        string[] SeedNodePaths { get; }
        
        /// <summary>
        /// Each role is a unique string that defines a role that allows this node
        /// to be routed to or to identify it so that work can be distributed to it
        /// 
        /// The value of the dictionary is the minimum number of nodes with the given
        /// role that must be in the cluster to consider it up
        /// </summary>
        /// <returns></returns>
        Dictionary<string, int> Roles { get; }
    }
}