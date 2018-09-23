using Akka.Actor;
using AkkaLibrary.Cluster.Actors;
using AkkaLibrary.Common.Interfaces;

namespace AkkaLibrary.Cluster.Configuration
{
    /// <summary>
    /// The default factory used to create <see cref="Props"/> for
    /// nodes in the cluster.
    /// </summary>
    public class DefaultClusterPropsFactory : IPropsFactory
    {
        /// <summary>
        /// Create the actor props for a <see cref="PluginManager"/>
        /// </summary>
        /// <returns><see cref="Props"/></returns>
        public Props CreateProps()
        {
            return Props.Create(() => new PluginManager());
        }
    }
}